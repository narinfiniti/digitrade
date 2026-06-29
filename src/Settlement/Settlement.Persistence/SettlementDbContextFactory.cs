using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Settlement.Persistence;

public sealed class SettlementDbContextFactory : IDesignTimeDbContextFactory<SettlementDbContext>
{
    public SettlementDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("SETTLEMENT_DB_CONNECTION");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("SETTLEMENT_DB_CONNECTION must be configured for Settlement design-time tooling.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<SettlementDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", SettlementDbContext.DefaultSchema));

        return new SettlementDbContext(optionsBuilder.Options);
    }
}