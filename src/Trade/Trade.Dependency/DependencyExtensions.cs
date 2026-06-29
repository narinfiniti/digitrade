using DigiTrade.Common.Projections;
using DigiTrade.Observability.HealthChecks;
using DigiTrade.Messaging.Configuration;
using DigiTrade.Messaging.Contracts;
using DigiTrade.Messaging.Publishers;
using DigiTrade.Messaging.Persistence.Outbox;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Trade.Application.Abstractions;
using Trade.Application.UseCases;
using Trade.Domain.Trades;
using Trade.Infrastructure.Outbox;
using Trade.Persistence;
using Trade.Persistence.Outbox;
using Trade.Persistence.Trades;
using Microsoft.AspNetCore.Builder;

namespace Trade.Dependency;

public static class DependencyExtensions
{
    public static IServiceCollection AddDependencyServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddSingleton<ITradeService, TradeService>();
        services.AddSingleton<ILocalDomainEventProjectionStore, InMemoryLocalDomainEventProjectionStore>();
        services.AddDbContext<TradeDbContext>(options =>
            options.UseNpgsql(
                GetTradeConnectionString(configuration),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", TradeDbContext.DefaultSchema)));
        services.AddScoped<ITradeRepository, TradeRepository>();
        services.AddScoped<TradeOutboxStore>();
        services.AddScoped<ITradeOutboxWriter>(sp => sp.GetRequiredService<TradeOutboxStore>());
        services.AddScoped<IOutboxStore>(sp => sp.GetRequiredService<TradeOutboxStore>());
        services.AddScoped<ITradeOutboxPublisher, TradeOutboxPublisher>();
        services.AddOptions<KafkaIntegrationPublisherOptions>()
            .Bind(configuration.GetSection(KafkaIntegrationPublisherOptions.SectionName))
            .PostConfigure(options =>
            {
                options.TopicName = string.IsNullOrWhiteSpace(options.TopicName) || options.TopicName == "digitrade.domain-events"
                    ? "digitrade.trade-events"
                    : options.TopicName;
                options.ClientId = string.IsNullOrWhiteSpace(options.ClientId) || options.ClientId == "digitrade-domain-events"
                    ? "trade-outbox-publisher"
                    : options.ClientId;
            })
            .Validate(options => !string.IsNullOrWhiteSpace(options.BootstrapServers), $"{KafkaIntegrationPublisherOptions.SectionName}:BootstrapServers must not be empty.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.TopicName), $"{KafkaIntegrationPublisherOptions.SectionName}:TopicName must not be empty.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.ClientId), $"{KafkaIntegrationPublisherOptions.SectionName}:ClientId must not be empty.")
            .ValidateOnStart();
        services.AddSingleton<IIntegrationEventPublisher, KafkaIntegrationEventPublisher>();
        services.AddMediatR(registration => registration.RegisterServicesFromAssembly(typeof(OpenTradeCommand).Assembly));

        services.AddTransient<IValidator<OpenTradeCommand.Model>, OpenTradeCommandValidator>();
        services.AddTransient<IValidator<CloseTradeCommand.Model>, CloseTradeCommandValidator>();
        services.AddTransient<IValidator<GetTradeByIdQuery.Model>, GetTradeByIdQueryValidator>();
        services.AddDigiTradeHealthChecks();

        return services;
    }

    private static string GetTradeConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Trade");

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        connectionString = configuration["TRADE_DB_CONNECTION"];

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        throw new InvalidOperationException("TRADE_DB_CONNECTION or ConnectionStrings:Trade must be configured for Trade persistence.");
    }

    public static async Task<WebApplication> EnsureTradeDatabaseMigratedAsync(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TradeDbContext>();
        await dbContext.Database.MigrateAsync();

        return app;
    }
}