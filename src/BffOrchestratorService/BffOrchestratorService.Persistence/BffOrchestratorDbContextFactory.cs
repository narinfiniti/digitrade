using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BffOrchestratorService.Persistence;

public sealed class BffOrchestratorDbContextFactory : IDesignTimeDbContextFactory<BffOrchestratorDbContext>
{
    public BffOrchestratorDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("BFF_ORCHESTRATOR_DB_CONNECTION");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("BFF_ORCHESTRATOR_DB_CONNECTION must be configured for BffOrchestratorService design-time tooling.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<BffOrchestratorDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", BffOrchestratorDbContext.DefaultSchema));

        return new BffOrchestratorDbContext(optionsBuilder.Options);
    }
}