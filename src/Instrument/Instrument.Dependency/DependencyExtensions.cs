using DigiTrade.Observability.HealthChecks;
using Instrument.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;

namespace Instrument.Dependency;

public static class DependencyExtensions
{
    public static IServiceCollection AddDependencyServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddDbContext<InstrumentDbContext>(options =>
            options.UseNpgsql(
                GetInstrumentConnectionString(configuration),
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", InstrumentDbContext.DefaultSchema)));
        services.AddDigiTradeHealthChecks();

        return services;
    }

    private static string GetInstrumentConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Instrument");

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        connectionString = configuration["INSTRUMENT_DB_CONNECTION"];

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        throw new InvalidOperationException("INSTRUMENT_DB_CONNECTION or ConnectionStrings:Instrument must be configured for Instrument persistence.");
    }

    public static async Task<WebApplication> EnsureInstrumentDatabaseMigratedAsync(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InstrumentDbContext>();
        await dbContext.Database.MigrateAsync();

        return app;
    }
}