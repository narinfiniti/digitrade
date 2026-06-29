using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Portfolio.Persistence;

public sealed class PortfolioDbContextFactory : IDesignTimeDbContextFactory<PortfolioDbContext>
{
    public PortfolioDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("PORTFOLIO_DB_CONNECTION");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("PORTFOLIO_DB_CONNECTION must be set when creating PortfolioDbContext at design time.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<PortfolioDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", PortfolioDbContext.DefaultSchema));

        return new PortfolioDbContext(optionsBuilder.Options);
    }
}
