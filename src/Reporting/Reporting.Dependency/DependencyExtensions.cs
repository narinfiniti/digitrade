using DigiTrade.Observability.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DigiTrade.Messaging.Contracts;
using DigiTrade.Messaging.Persistence.Snapshots;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Reporting.Domain.Projections;
using Reporting.Infrastructure.Consumers;
using Reporting.Infrastructure.Events;
using Reporting.Infrastructure.Options;
using Reporting.Persistence;
using Reporting.Persistence.Stores;

namespace Reporting.Dependency;

public static class DependencyExtensions
{
    public static IServiceCollection AddDependencyServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<TradeExecutionCompletedConsumerOptions>()
            .Bind(configuration.GetSection(TradeExecutionCompletedConsumerOptions.SectionName))
            .ValidateDataAnnotations();

        services.AddDbContext<ReportingDbContext>(options =>
            options.UseNpgsql(
                GetReportingConnectionString(configuration),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", ReportingDbContext.DefaultSchema)));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IEventSnapshotStore, InMemoryEventSnapshotStore>();
        services.AddSingleton<ITradeExecutionReportProjectionStore, InMemoryTradeExecutionReportProjectionStore>();
        services.AddTransient<IIntegrationEventConsumer<TradeExecutionCompletedEvent>, TradeExecutionCompletedConsumer>();

        services.AddDigiTradeHealthChecks();

        return services;
    }

    private static string GetReportingConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Reporting");

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        connectionString = configuration["REPORTING_DB_CONNECTION"];

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        throw new InvalidOperationException("REPORTING_DB_CONNECTION or ConnectionStrings:Reporting must be configured for Reporting persistence.");
    }

    public static async Task<WebApplication> EnsureReportingDatabaseMigratedAsync(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ReportingDbContext>();
        await dbContext.Database.MigrateAsync();

        return app;
    }
}