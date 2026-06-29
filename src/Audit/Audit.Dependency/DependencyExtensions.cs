using Audit.Domain.Evidence;
using Audit.Infrastructure.Consumers;
using Audit.Infrastructure.Events;
using Audit.Infrastructure.Options;
using Audit.Persistence.Stores;
using DigiTrade.Messaging.Contracts;
using DigiTrade.Messaging.Persistence.Snapshots;
using DigiTrade.Observability.HealthChecks;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Audit.Persistence;

namespace Audit.Dependency;

public static class DependencyExtensions
{
    public static IServiceCollection AddDependencyServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<BusinessProcessCompletedConsumerOptions>()
            .Bind(configuration.GetSection(BusinessProcessCompletedConsumerOptions.SectionName))
            .ValidateDataAnnotations();

        services.AddDbContext<AuditDbContext>(options =>
            options.UseNpgsql(
                GetAuditConnectionString(configuration),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", AuditDbContext.DefaultSchema)));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IEventSnapshotStore, InMemoryEventSnapshotStore>();
        services.AddSingleton<IAuditEvidenceRecordStore, InMemoryAuditEvidenceRecordStore>();
        services.AddTransient<IIntegrationEventConsumer<BusinessProcessCompletedEvent>, BusinessProcessCompletedAuditConsumer>();

        services.AddDigiTradeHealthChecks();

        return services;
    }

    private static string GetAuditConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Audit");

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        connectionString = configuration["AUDIT_DB_CONNECTION"];

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        throw new InvalidOperationException("AUDIT_DB_CONNECTION or ConnectionStrings:Audit must be configured for Audit persistence.");
    }

    public static async Task<WebApplication> EnsureAuditDatabaseMigratedAsync(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        await dbContext.Database.MigrateAsync();

        return app;
    }
}