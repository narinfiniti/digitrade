using BffOrchestratorService.Domain.Models;

namespace BffOrchestratorService.Infrastructure.Abstractions;

public interface IDownstreamOrchestrationProbeClient
{
    string ServiceName { get; }

    Task<OrchestrationDependencyStatusModel> GetStatusAsync(string correlationId, CancellationToken cancellationToken = default);
}