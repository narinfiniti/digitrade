using Microsoft.EntityFrameworkCore;

namespace Instrument.Persistence;

public sealed class InstrumentDbContext(DbContextOptions<InstrumentDbContext> options) : DbContext(options)
{
    public const string DefaultSchema = "instrument";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema(DefaultSchema);

        modelBuilder.Entity<SchemaMarker>(entity =>
        {
            entity.ToTable("instrument_catalog");
            entity.HasKey(marker => marker.Id);
            entity.Property(marker => marker.Id).ValueGeneratedNever();
            entity.Property(marker => marker.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }

    private sealed class SchemaMarker
    {
        public int Id { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
