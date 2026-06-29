using Microsoft.EntityFrameworkCore;

namespace Position.Persistence;

public sealed class PositionDbContext(DbContextOptions<PositionDbContext> options) : DbContext(options)
{
    public const string DefaultSchema = "position";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema(DefaultSchema);

        modelBuilder.Entity<SchemaMarker>(entity =>
        {
            entity.ToTable("position_snapshots");
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
