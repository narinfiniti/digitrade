using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Reporting.Persistence;

public sealed class ReportingDbContextFactory : IDesignTimeDbContextFactory<ReportingDbContext>
{
    public ReportingDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("REPORTING_DB_CONNECTION");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("REPORTING_DB_CONNECTION must be set when creating ReportingDbContext at design time.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<ReportingDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", ReportingDbContext.DefaultSchema));

        return new ReportingDbContext(optionsBuilder.Options);
    }
}
