using BffOrchestratorService.Domain.Entities;
using BffOrchestratorService.Persistence.EntityTypeConfigs;
using Microsoft.EntityFrameworkCore;

namespace BffOrchestratorService.Persistence;

public sealed class BffOrchestratorDbContext(DbContextOptions<BffOrchestratorDbContext> options) : DbContext(options)
{
    public const string DefaultSchema = "bfforchestrator";

    public DbSet<BusinessProcessState> BusinessProcessStates => Set<BusinessProcessState>();

    public DbSet<ProcessCheckpoint> ProcessCheckpoints => Set<ProcessCheckpoint>();

    public DbSet<ProcessQueueItem> ProcessQueueItems => Set<ProcessQueueItem>();

    public DbSet<ProcessRuntimeOutboxMessage> ProcessRuntimeOutboxMessages => Set<ProcessRuntimeOutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema(DefaultSchema);
        modelBuilder.ApplyConfiguration(new BusinessProcessStateEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new ProcessCheckpointEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new ProcessQueueItemEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new ProcessRuntimeOutboxMessageEntityTypeConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}
