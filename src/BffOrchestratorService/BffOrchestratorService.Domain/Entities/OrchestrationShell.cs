using BffOrchestratorService.Domain.Models;
using DigiTrade.SharedKernel.Abstractions;
using DigiTrade.SharedKernel.Events;

namespace BffOrchestratorService.Domain.Entities;

public sealed class OrchestrationShell : IAggregateRoot<Guid>
{
    public Guid Id { get; internal set; }

    public string FlowName { get; internal set; } = string.Empty;

    public string CorrelationId { get; internal set; } = string.Empty;

    public string RequestedBySubjectId { get; internal set; } = string.Empty;

    public string RequestedByUserName { get; internal set; } = string.Empty;

    public string Status { get; internal set; } = string.Empty;

    public bool DependenciesHealthy { get; internal set; }

    public IReadOnlyCollection<OrchestrationDependencyStatusModel> Dependencies { get; internal set; } = Array.Empty<OrchestrationDependencyStatusModel>();

    public int Version { get; internal set; }

    public DateTimeOffset CreatedAt { get; internal set; }

    public DateTimeOffset UpdatedAt { get; internal set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents { get; internal set; } = Array.Empty<IDomainEvent>();

    public void ClearDomainEvents()
    {
        DomainEvents = Array.Empty<IDomainEvent>();
    }
}