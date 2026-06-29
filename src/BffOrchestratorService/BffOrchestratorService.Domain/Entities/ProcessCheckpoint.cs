using DigiTrade.SharedKernel.Abstractions;
using DigiTrade.SharedKernel.Events;

namespace BffOrchestratorService.Domain.Entities;

public sealed class ProcessCheckpoint : IAggregateRoot<long>
{
    public long Id { get; set; }

    public Guid ProcessId { get; set; }

    public int StepOrdinal { get; set; }

    public string StepName { get; set; } = string.Empty;

    public string CheckpointKind { get; set; } = string.Empty;

    public string ObservedOutcome { get; set; } = string.Empty;

    public Guid DispatchId { get; set; }

    public string IdempotencyKey { get; set; } = string.Empty;

    public string? MessageKey { get; set; }

    public string? ExternalReference { get; set; }

    public string CheckpointData { get; set; } = "{}";

    public DateTimeOffset OccurredAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public int Version { get; set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents { get; set; } = Array.Empty<IDomainEvent>();

    public void ClearDomainEvents()
    {
        DomainEvents = Array.Empty<IDomainEvent>();
    }
}
