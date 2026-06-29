using Ledger.Persistence.Ledgers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Ledger.Persistence;

public sealed class LedgerDbContextFactory : IDesignTimeDbContextFactory<LedgerDbContext>
{
    public LedgerDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("LEDGER_DB_CONNECTION");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("LEDGER_DB_CONNECTION must be configured for Ledger design-time tooling.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<LedgerDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", LedgerDbContext.DefaultSchema));

        return new LedgerDbContext(optionsBuilder.Options);
    }
}
