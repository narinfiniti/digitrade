using DigiTrade.Observability.HealthChecks;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Position.Persistence;

namespace Position.Dependency;

public static class DependencyExtensions
{
    public static IServiceCollection AddDependencyServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<PositionDbContext>(options =>
            options.UseNpgsql(
                GetPositionConnectionString(configuration),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", PositionDbContext.DefaultSchema)));
        services.AddDigiTradeHealthChecks();

        return services;
    }

    private static string GetPositionConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Position");

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        connectionString = configuration["POSITION_DB_CONNECTION"];

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        throw new InvalidOperationException("POSITION_DB_CONNECTION or ConnectionStrings:Position must be configured for Position persistence.");
    }

    public static async Task<WebApplication> EnsurePositionDatabaseMigratedAsync(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PositionDbContext>();
        await dbContext.Database.MigrateAsync();

        return app;
    }
}