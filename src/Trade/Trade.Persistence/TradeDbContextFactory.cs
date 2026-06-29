using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Trade.Persistence;

public sealed class TradeDbContextFactory : IDesignTimeDbContextFactory<TradeDbContext>
{
    public TradeDbContext CreateDbContext(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        var connectionString = Environment.GetEnvironmentVariable("TRADE_DB_CONNECTION");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("TRADE_DB_CONNECTION is required for Trade DbContext design-time operations.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<TradeDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", TradeDbContext.DefaultSchema));

        return new TradeDbContext(optionsBuilder.Options);
    }
}