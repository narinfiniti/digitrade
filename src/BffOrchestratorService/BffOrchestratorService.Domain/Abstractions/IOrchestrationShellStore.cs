using BffOrchestratorService.Domain.Entities;

namespace BffOrchestratorService.Domain.Abstractions;

public interface IOrchestrationShellStore
{
    Task AddAsync(OrchestrationShell orchestrationShell, CancellationToken cancellationToken = default);

    Task<OrchestrationShell?> GetAsync(Guid orchestrationShellId, CancellationToken cancellationToken = default);
}