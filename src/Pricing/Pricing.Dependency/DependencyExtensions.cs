using DigiTrade.Observability.HealthChecks;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pricing.Persistence;

namespace Pricing.Dependency;

public static class DependencyExtensions
{
    public static IServiceCollection AddDependencyServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<PricingDbContext>(options =>
            options.UseNpgsql(
                GetPricingConnectionString(configuration),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", PricingDbContext.DefaultSchema)));
        services.AddDigiTradeHealthChecks();

        return services;
    }

    private static string GetPricingConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Pricing");

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        connectionString = configuration["PRICING_DB_CONNECTION"];

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        throw new InvalidOperationException("PRICING_DB_CONNECTION or ConnectionStrings:Pricing must be configured for Pricing persistence.");
    }

    public static async Task<WebApplication> EnsurePricingDatabaseMigratedAsync(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PricingDbContext>();
        await dbContext.Database.MigrateAsync();

        return app;
    }
}