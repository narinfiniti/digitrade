using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Position.Persistence;

public sealed class PositionDbContextFactory : IDesignTimeDbContextFactory<PositionDbContext>
{
    public PositionDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("POSITION_DB_CONNECTION");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("POSITION_DB_CONNECTION must be set when creating PositionDbContext at design time.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<PositionDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", PositionDbContext.DefaultSchema));

        return new PositionDbContext(optionsBuilder.Options);
    }
}
