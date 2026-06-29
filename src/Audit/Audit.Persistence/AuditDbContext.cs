using Microsoft.EntityFrameworkCore;

namespace Audit.Persistence;

public sealed class AuditDbContext(DbContextOptions<AuditDbContext> options) : DbContext(options)
{
    public const string DefaultSchema = "audit";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema(DefaultSchema);

        modelBuilder.Entity<SchemaMarker>(entity =>
        {
            entity.ToTable("evidence_records");
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
