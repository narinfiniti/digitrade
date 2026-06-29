using BffOrchestratorService.Domain.Entities;

namespace BffOrchestratorService.Infrastructure.Abstractions;

public interface IOrchestrationShellService
{
    Task<OrchestrationShell> StartAsync(
        string flowName,
        string correlationId,
        string idempotencyKey,
        string requestedBySubjectId,
        string requestedByUserName,
        IReadOnlyCollection<string>? involvedServices = null,
        CancellationToken cancellationToken = default);

    Task<OrchestrationShell?> GetAsync(Guid orchestrationShellId, CancellationToken cancellationToken = default);
}