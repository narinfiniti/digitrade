using BffOrchestratorService.Domain.Models;

namespace BffOrchestratorService.Infrastructure.Abstractions;

public interface IProcessQueueWorkItemHandler
{
    bool CanHandle(ProcessQueueItemModel queueItem);

    Task HandleAsync(ProcessQueueItemModel queueItem, string leaseOwner, CancellationToken cancellationToken = default);
}