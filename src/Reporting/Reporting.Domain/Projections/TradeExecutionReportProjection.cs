using DigiTrade.SharedKernel.Abstractions;
using DigiTrade.SharedKernel.Events;

namespace Reporting.Domain.Projections;

public sealed class TradeExecutionReportProjection : IAggregateRoot<Guid>
{
    public Guid Id { get; internal set; }

    public string AggregateId { get; internal set; } = string.Empty;

    public string BusinessProcessId { get; internal set; } = string.Empty;

    public string CompletionStatus { get; internal set; } = string.Empty;

    public decimal FilledQuantity { get; internal set; }

    public decimal AveragePrice { get; internal set; }

    public decimal Notional { get; internal set; }

    public string CorrelationId { get; internal set; } = string.Empty;

    public Guid LastEventId { get; internal set; }

    public string LastEventName { get; internal set; } = string.Empty;

    public int LastEventVersion { get; internal set; }

    public DateTimeOffset LastOccurredAtUtc { get; internal set; }

    public int Version { get; internal set; }

    public DateTimeOffset CreatedAt { get; internal set; }

    public DateTimeOffset UpdatedAt { get; internal set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents { get; internal set; } = Array.Empty<IDomainEvent>();

    public void ClearDomainEvents()
    {
        DomainEvents = Array.Empty<IDomainEvent>();
    }
}