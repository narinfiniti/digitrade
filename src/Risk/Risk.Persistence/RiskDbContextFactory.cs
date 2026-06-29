using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Risk.Persistence;

public sealed class RiskDbContextFactory : IDesignTimeDbContextFactory<RiskDbContext>
{
    public RiskDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("RISK_DB_CONNECTION");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("RISK_DB_CONNECTION must be configured for Risk design-time tooling.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<RiskDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", RiskDbContext.DefaultSchema));

        return new RiskDbContext(optionsBuilder.Options);
    }
}