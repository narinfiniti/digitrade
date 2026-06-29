using DigiTrade.Observability.HealthChecks;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Portfolio.Persistence;

namespace Portfolio.Dependency;

public static class DependencyExtensions
{
    public static IServiceCollection AddDependencyServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<PortfolioDbContext>(options =>
            options.UseNpgsql(
                GetPortfolioConnectionString(configuration),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", PortfolioDbContext.DefaultSchema)));
        services.AddDigiTradeHealthChecks();

        return services;
    }

    private static string GetPortfolioConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Portfolio");

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        connectionString = configuration["PORTFOLIO_DB_CONNECTION"];

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        throw new InvalidOperationException("PORTFOLIO_DB_CONNECTION or ConnectionStrings:Portfolio must be configured for Portfolio persistence.");
    }

    public static async Task<WebApplication> EnsurePortfolioDatabaseMigratedAsync(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PortfolioDbContext>();
        await dbContext.Database.MigrateAsync();

        return app;
    }
}