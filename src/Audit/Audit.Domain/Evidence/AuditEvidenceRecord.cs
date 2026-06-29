using DigiTrade.SharedKernel.Abstractions;
using DigiTrade.SharedKernel.Events;

namespace Audit.Domain.Evidence;

public sealed class AuditEvidenceRecord : IAggregateRoot<Guid>
{
    public Guid Id { get; internal set; }

    public Guid EventId { get; internal set; }

    public string EventName { get; internal set; } = string.Empty;

    public int EventVersion { get; internal set; }

    public string AggregateId { get; internal set; } = string.Empty;

    public string BusinessProcessId { get; internal set; } = string.Empty;

    public string Status { get; internal set; } = string.Empty;

    public string CorrelationId { get; internal set; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; internal set; }

    public string PayloadJson { get; internal set; } = string.Empty;

    public int Version { get; internal set; }

    public DateTimeOffset CreatedAt { get; internal set; }

    public DateTimeOffset UpdatedAt { get; internal set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents { get; internal set; } = Array.Empty<IDomainEvent>();

    public void ClearDomainEvents()
    {
        DomainEvents = Array.Empty<IDomainEvent>();
    }
}
