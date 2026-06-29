using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Instrument.Persistence;

public sealed class InstrumentDbContextFactory : IDesignTimeDbContextFactory<InstrumentDbContext>
{
    public InstrumentDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("INSTRUMENT_DB_CONNECTION");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("INSTRUMENT_DB_CONNECTION must be set when creating InstrumentDbContext at design time.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<InstrumentDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", InstrumentDbContext.DefaultSchema));

        return new InstrumentDbContext(optionsBuilder.Options);
    }
}
