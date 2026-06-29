using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BffNotificationService.Persistence;

public sealed class BffNotificationDbContextFactory : IDesignTimeDbContextFactory<BffNotificationDbContext>
{
    public BffNotificationDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("BFF_NOTIFICATION_DB_CONNECTION");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("BFF_NOTIFICATION_DB_CONNECTION must be set when creating BffNotificationDbContext at design time.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<BffNotificationDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", BffNotificationDbContext.DefaultSchema));

        return new BffNotificationDbContext(optionsBuilder.Options);
    }
}
