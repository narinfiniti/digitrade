using DigiTrade.Observability.HealthChecks;
using Account.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;

namespace Account.Dependency;

public static class DependencyExtensions
{
    public static IServiceCollection AddDependencyServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<AccountDbContext>(options =>
            options.UseNpgsql(
                GetAccountConnectionString(configuration),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", AccountDbContext.DefaultSchema)));
        services.AddDigiTradeHealthChecks();

        return services;
    }

    private static string GetAccountConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Account");

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        connectionString = configuration["ACCOUNT_DB_CONNECTION"];

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        throw new InvalidOperationException("ACCOUNT_DB_CONNECTION or ConnectionStrings:Account must be configured for Account persistence.");
    }

    public static async Task<WebApplication> EnsureAccountDatabaseMigratedAsync(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AccountDbContext>();
        await dbContext.Database.MigrateAsync();

        return app;
    }
}