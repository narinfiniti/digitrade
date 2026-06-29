using DigiTrade.Common.Projections;
using DigiTrade.Messaging.Configuration;
using DigiTrade.Messaging.Contracts;
using DigiTrade.Messaging.Publishers;
using DigiTrade.Messaging.Persistence.Outbox;
using DigiTrade.Observability.HealthChecks;
using Ledger.Application.Abstractions;
using Ledger.Domain.Ledgers;
using Ledger.Infrastructure.Consumers;
using Ledger.Infrastructure.Outbox;
using Ledger.Persistence;
using Ledger.Persistence.Ledgers;
using Ledger.Application.EventHandlers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using MediatR;
using Settlement.Application.Events;

namespace Ledger.Dependency;

public static class DependencyExtensions
{
    public static IServiceCollection AddDependencyServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddSingleton<ILedgerService, LedgerService>();
        services.AddSingleton<ILocalDomainEventProjectionStore, InMemoryLocalDomainEventProjectionStore>();
        services.AddDbContext<LedgerDbContext>(options =>
            options.UseNpgsql(
                GetLedgerConnectionString(configuration),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", LedgerDbContext.DefaultSchema)));
        services.AddScoped<ILedgerEntryRepository, LedgerRepository>();
        services.AddScoped<LedgerOutboxStore>();
        services.AddScoped<IOutboxStore>(sp => sp.GetRequiredService<LedgerOutboxStore>());
        services.AddScoped<ILedgerOutboxPublisher, LedgerOutboxPublisher>();
        services.AddOptions<KafkaIntegrationPublisherOptions>()
            .Bind(configuration.GetSection(KafkaIntegrationPublisherOptions.SectionName))
            .PostConfigure(options =>
            {
                options.TopicName = string.IsNullOrWhiteSpace(options.TopicName) || options.TopicName == "digitrade.domain-events"
                    ? "digitrade.ledger-events"
                    : options.TopicName;
                options.ClientId = string.IsNullOrWhiteSpace(options.ClientId) || options.ClientId == "digitrade-domain-events"
                    ? "ledger-outbox-publisher"
                    : options.ClientId;
            })
            .Validate(options => !string.IsNullOrWhiteSpace(options.BootstrapServers), $"{KafkaIntegrationPublisherOptions.SectionName}:BootstrapServers must not be empty.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.TopicName), $"{KafkaIntegrationPublisherOptions.SectionName}:TopicName must not be empty.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.ClientId), $"{KafkaIntegrationPublisherOptions.SectionName}:ClientId must not be empty.")
            .ValidateOnStart();
        services.AddSingleton<IIntegrationEventPublisher, KafkaIntegrationEventPublisher>();
        services.AddMediatR(registration => registration.RegisterServicesFromAssembly(typeof(LedgerEntryPostedDomainEventProjectionHandler).Assembly));
        services.AddTransient<IIntegrationEventConsumer<SettlementFinalizedIntegrationEvent>, SettlementFinalizedConsumer>();
        services.AddDigiTradeHealthChecks();

        return services;
    }

    private static string GetLedgerConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Ledger");

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        connectionString = configuration["LEDGER_DB_CONNECTION"];

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        throw new InvalidOperationException("LEDGER_DB_CONNECTION or ConnectionStrings:Ledger must be configured for Ledger persistence.");
    }

    public static async Task<WebApplication> EnsureLedgerDatabaseMigratedAsync(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LedgerDbContext>();
        await dbContext.Database.MigrateAsync();

        return app;
    }
}