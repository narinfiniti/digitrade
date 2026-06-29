using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Account.Persistence;

public sealed class AccountDbContextFactory : IDesignTimeDbContextFactory<AccountDbContext>
{
    public AccountDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ACCOUNT_DB_CONNECTION");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("ACCOUNT_DB_CONNECTION must be set when creating AccountDbContext at design time.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<AccountDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", AccountDbContext.DefaultSchema));

        return new AccountDbContext(optionsBuilder.Options);
    }
}
