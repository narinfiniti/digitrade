using BffOrchestratorService.Domain.Models;

namespace BffOrchestratorService.Domain.Abstractions;

public interface IProcessCheckpointStore
{
    Task AddAsync(ProcessCheckpointModel checkpoint, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid processId, string checkpointKind, string idempotencyKey, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ProcessCheckpointModel>> ListByProcessIdAsync(Guid processId, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
