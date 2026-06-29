using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Audit.Persistence;

public sealed class AuditDbContextFactory : IDesignTimeDbContextFactory<AuditDbContext>
{
    public AuditDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("AUDIT_DB_CONNECTION");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("AUDIT_DB_CONNECTION must be set when creating AuditDbContext at design time.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<AuditDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", AuditDbContext.DefaultSchema));

        return new AuditDbContext(optionsBuilder.Options);
    }
}
