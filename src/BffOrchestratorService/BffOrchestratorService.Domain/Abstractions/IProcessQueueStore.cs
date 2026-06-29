using BffOrchestratorService.Domain.Models;

namespace BffOrchestratorService.Domain.Abstractions;

public interface IProcessQueueStore
{
    Task AddAsync(ProcessQueueItemModel queueItem, CancellationToken cancellationToken = default);

    Task UpdateAsync(ProcessQueueItemModel queueItem, CancellationToken cancellationToken = default);

    Task<ProcessQueueItemModel?> FindByDedupeKeyAsync(string dedupeKey, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ProcessQueueItemModel>> LeaseReadyAsync(
        int batchSize,
        DateTimeOffset asOfUtc,
        string leaseOwner,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default);

    Task<bool> RenewLeaseAsync(
        long queueItemId,
        DateTimeOffset asOfUtc,
        string leaseOwner,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default);

    Task<bool> CompleteAsync(
        long queueItemId,
        DateTimeOffset asOfUtc,
        string leaseOwner,
        CancellationToken cancellationToken = default);

    Task<bool> DeadLetterAsync(
        long queueItemId,
        DateTimeOffset asOfUtc,
        string leaseOwner,
        string errorCode,
        string errorMessage,
        CancellationToken cancellationToken = default);

    Task<bool> RequeueAsync(
        long queueItemId,
        DateTimeOffset asOfUtc,
        DateTimeOffset visibleAt,
        string leaseOwner,
        string errorCode,
        string errorMessage,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<ProcessQueueItemModel>> ListVisibleAsync(int batchSize, DateTimeOffset asOfUtc, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
