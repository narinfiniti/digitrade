using Microsoft.EntityFrameworkCore;

namespace Pricing.Persistence;

public sealed class PricingDbContext(DbContextOptions<PricingDbContext> options) : DbContext(options)
{
    public const string DefaultSchema = "pricing";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema(DefaultSchema);

        modelBuilder.Entity<SchemaMarker>(entity =>
        {
            entity.ToTable("price_snapshots");
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
