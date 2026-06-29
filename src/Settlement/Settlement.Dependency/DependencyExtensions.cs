using DigiTrade.Common.Projections;
using DigiTrade.Messaging.Configuration;
using DigiTrade.Messaging.Contracts;
using DigiTrade.Messaging.Publishers;
using DigiTrade.Messaging.Persistence.Outbox;
using DigiTrade.Observability.HealthChecks;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Settlement.Application.Abstractions;
using Settlement.Application.UseCases;
using Settlement.Domain.Settlements;
using Settlement.Infrastructure.Outbox;
using Settlement.Persistence;
using Settlement.Persistence.Outbox;
using Settlement.Persistence.Settlements;
using Microsoft.AspNetCore.Builder;

namespace Settlement.Dependency;

public static class DependencyExtensions
{
    public static IServiceCollection AddDependencyServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddSingleton<ISettlementService, SettlementService>();
        services.AddSingleton<ILocalDomainEventProjectionStore, InMemoryLocalDomainEventProjectionStore>();
        services.AddDbContext<SettlementDbContext>(options =>
            options.UseNpgsql(
                GetSettlementConnectionString(configuration),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", SettlementDbContext.DefaultSchema)));
        services.AddScoped<ISettlementRepository, SettlementRepository>();
        services.AddScoped<SettlementOutboxStore>();
        services.AddScoped<ISettlementOutboxWriter>(sp => sp.GetRequiredService<SettlementOutboxStore>());
        services.AddScoped<IOutboxStore>(sp => sp.GetRequiredService<SettlementOutboxStore>());
        services.AddScoped<ISettlementOutboxPublisher, SettlementOutboxPublisher>();
        services.AddOptions<KafkaIntegrationPublisherOptions>()
            .Bind(configuration.GetSection(KafkaIntegrationPublisherOptions.SectionName))
            .PostConfigure(options =>
            {
                options.TopicName = string.IsNullOrWhiteSpace(options.TopicName) || options.TopicName == "digitrade.domain-events"
                    ? "digitrade.settlement-events"
                    : options.TopicName;
                options.ClientId = string.IsNullOrWhiteSpace(options.ClientId) || options.ClientId == "digitrade-domain-events"
                    ? "settlement-outbox-publisher"
                    : options.ClientId;
            })
            .Validate(options => !string.IsNullOrWhiteSpace(options.BootstrapServers), $"{KafkaIntegrationPublisherOptions.SectionName}:BootstrapServers must not be empty.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.TopicName), $"{KafkaIntegrationPublisherOptions.SectionName}:TopicName must not be empty.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.ClientId), $"{KafkaIntegrationPublisherOptions.SectionName}:ClientId must not be empty.")
            .ValidateOnStart();
        services.AddSingleton<IIntegrationEventPublisher, KafkaIntegrationEventPublisher>();
        services.AddMediatR(registration => registration.RegisterServicesFromAssembly(typeof(InitiateSettlementCommand).Assembly));

        services.AddTransient<IValidator<InitiateSettlementCommand.Model>, InitiateSettlementCommandValidator>();
        services.AddTransient<IValidator<GetSettlementByIdQuery.Model>, GetSettlementByIdQueryValidator>();
        services.AddTransient<IValidator<FinalizeSettlementCommand.Model>, FinalizeSettlementCommandValidator>();
        services.AddTransient<IValidator<FailSettlementCommand.Model>, FailSettlementCommandValidator>();
        services.AddDigiTradeHealthChecks();

        return services;
    }

    private static string GetSettlementConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Settlement");

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        connectionString = configuration["SETTLEMENT_DB_CONNECTION"];

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        throw new InvalidOperationException("SETTLEMENT_DB_CONNECTION or ConnectionStrings:Settlement must be configured for Settlement persistence.");
    }

    public static async Task<WebApplication> EnsureSettlementDatabaseMigratedAsync(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SettlementDbContext>();
        await dbContext.Database.MigrateAsync();

        return app;
    }
}