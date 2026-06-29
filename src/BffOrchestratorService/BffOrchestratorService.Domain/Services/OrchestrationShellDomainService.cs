using BffOrchestratorService.Domain.Entities;
using BffOrchestratorService.Domain.Models;
using DigiTrade.SharedKernel.Abstractions;

namespace BffOrchestratorService.Domain.Services;

public sealed class OrchestrationShellDomainService : IDomainService
{
  public OrchestrationShell Create(
        string flowName,
        string correlationId,
        string requestedBySubjectId,
        string requestedByUserName,
        bool dependenciesHealthy,
        IReadOnlyCollection<OrchestrationDependencyStatusModel> dependencies,
        DateTimeOffset createdAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(flowName);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedBySubjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedByUserName);
        ArgumentNullException.ThrowIfNull(dependencies);

        return new OrchestrationShell
        {
            Id = Guid.NewGuid(),
            FlowName = flowName,
            CorrelationId = correlationId,
            RequestedBySubjectId = requestedBySubjectId,
            RequestedByUserName = requestedByUserName,
            Status = dependenciesHealthy ? "Accepted" : "PendingDependencies",
            DependenciesHealthy = dependenciesHealthy,
            Dependencies = dependencies,
            Version = 1,
            CreatedAt = createdAtUtc,
            UpdatedAt = createdAtUtc,
        };
    }

    public OrchestrationShell Restore(
        Guid orchestrationShellId,
        string flowName,
        string correlationId,
        string requestedBySubjectId,
        string requestedByUserName,
        string status,
        bool dependenciesHealthy,
        IReadOnlyCollection<OrchestrationDependencyStatusModel> dependencies,
        int version,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(version);
        ArgumentException.ThrowIfNullOrWhiteSpace(flowName);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedBySubjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedByUserName);
        ArgumentException.ThrowIfNullOrWhiteSpace(status);
        ArgumentNullException.ThrowIfNull(dependencies);

        return new OrchestrationShell
        {
            Id = orchestrationShellId,
            FlowName = flowName,
            CorrelationId = correlationId,
            RequestedBySubjectId = requestedBySubjectId,
            RequestedByUserName = requestedByUserName,
            Status = status,
            DependenciesHealthy = dependenciesHealthy,
            Dependencies = dependencies,
            Version = version,
            CreatedAt = createdAtUtc,
            UpdatedAt = updatedAtUtc,
        };
    }
}