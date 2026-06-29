using BffNotificationService.Domain.Notifications;
using BffNotificationService.Infrastructure.Abstractions;
using BffNotificationService.Infrastructure.Consumers;
using BffNotificationService.Infrastructure.Deliveries;
using BffNotificationService.Infrastructure.Events;
using BffNotificationService.Infrastructure.Options;
using BffNotificationService.Persistence.Stores;
using DigiTrade.Messaging.Contracts;
using DigiTrade.Messaging.Persistence.Snapshots;
using DigiTrade.Observability.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BffNotificationService.Persistence;

namespace BffNotificationService.Dependency;

public static class DependencyExtensions
{
    public static IServiceCollection AddDependencyServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<TerminalNotificationConsumerOptions>()
            .Bind(configuration.GetSection(TerminalNotificationConsumerOptions.SectionName))
            .ValidateDataAnnotations();

        services.AddDbContext<BffNotificationDbContext>(options =>
            options.UseNpgsql(
                GetBffNotificationConnectionString(configuration),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", BffNotificationDbContext.DefaultSchema)));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IEventSnapshotStore, InMemoryEventSnapshotStore>();
        services.AddSingleton<INotificationDeliveryStore, InMemoryNotificationDeliveryStore>();
        services.AddSingleton<INotificationWebSocketConnectionService, InMemoryNotificationWebSocketConnectionService>();
        services.AddSingleton<INotificationClientDeliveryService, InMemoryNotificationClientDeliveryService>();
        services.AddTransient<IIntegrationEventConsumer<TerminalNotificationRequestedEvent>, TerminalNotificationRequestedConsumer>();

        services.AddDigiTradeHealthChecks();

        return services;
    }

    private static string GetBffNotificationConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("BffNotification");

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        connectionString = configuration["BFF_NOTIFICATION_DB_CONNECTION"];

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        throw new InvalidOperationException("BFF_NOTIFICATION_DB_CONNECTION or ConnectionStrings:BffNotification must be configured for BffNotification persistence.");
    }
}