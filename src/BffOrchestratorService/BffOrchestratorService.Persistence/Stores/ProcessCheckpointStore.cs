using BffOrchestratorService.Domain;
using BffOrchestratorService.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace BffOrchestratorService.Persistence.Stores;

public sealed class ProcessCheckpointStore(BffOrchestratorDbContext dbContext) : IProcessCheckpointStore
{
    public async Task AddAsync(Domain.Models.ProcessCheckpointModel checkpoint, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(checkpoint);

        await dbContext.ProcessCheckpoints.AddAsync(ToEntity(checkpoint), cancellationToken);
    }

    public Task<bool> ExistsAsync(
        Guid processId,
        string checkpointKind,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        return dbContext.ProcessCheckpoints.AnyAsync(
            checkpoint => checkpoint.ProcessId == processId
                && checkpoint.CheckpointKind == checkpointKind
                && checkpoint.IdempotencyKey == idempotencyKey,
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<Domain.Models.ProcessCheckpointModel>> ListByProcessIdAsync(
        Guid processId,
        CancellationToken cancellationToken = default)
    {
        var entities = await dbContext.ProcessCheckpoints
            .AsNoTracking()
            .Where(checkpoint => checkpoint.ProcessId == processId)
            .OrderBy(checkpoint => checkpoint.StepOrdinal)
            .ThenBy(checkpoint => checkpoint.CreatedAt)
            .ToListAsync(cancellationToken);

        return entities.Select(ToModel).ToArray();
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Domain.Entities.ProcessCheckpoint ToEntity(Domain.Models.ProcessCheckpointModel checkpoint)
    {
        return new Domain.Entities.ProcessCheckpoint
        {
            Id = checkpoint.Id,
            ProcessId = checkpoint.ProcessId,
            StepOrdinal = checkpoint.StepOrdinal,
            StepName = checkpoint.StepName,
            CheckpointKind = checkpoint.CheckpointKind,
            ObservedOutcome = checkpoint.ObservedOutcome,
            DispatchId = checkpoint.DispatchId,
            IdempotencyKey = checkpoint.IdempotencyKey,
            MessageKey = checkpoint.MessageKey,
            ExternalReference = checkpoint.ExternalReference,
            CheckpointData = checkpoint.CheckpointData,
            OccurredAt = checkpoint.OccurredAt,
            CreatedAt = checkpoint.CreatedAt,
        };
    }

    private static Domain.Models.ProcessCheckpointModel ToModel(Domain.Entities.ProcessCheckpoint entity)
    {
        return new Domain.Models.ProcessCheckpointModel
        {
            Id = entity.Id,
            ProcessId = entity.ProcessId,
            StepOrdinal = entity.StepOrdinal,
            StepName = entity.StepName,
            CheckpointKind = entity.CheckpointKind,
            ObservedOutcome = entity.ObservedOutcome,
            DispatchId = entity.DispatchId,
            IdempotencyKey = entity.IdempotencyKey,
            MessageKey = entity.MessageKey,
            ExternalReference = entity.ExternalReference,
            CheckpointData = entity.CheckpointData,
            OccurredAt = entity.OccurredAt,
            CreatedAt = entity.CreatedAt,
        };
    }
}
