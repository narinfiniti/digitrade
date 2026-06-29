using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Pricing.Persistence;

public sealed class PricingDbContextFactory : IDesignTimeDbContextFactory<PricingDbContext>
{
    public PricingDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("PRICING_DB_CONNECTION");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("PRICING_DB_CONNECTION must be set when creating PricingDbContext at design time.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<PricingDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", PricingDbContext.DefaultSchema));

        return new PricingDbContext(optionsBuilder.Options);
    }
}
