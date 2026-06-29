using DigiTrade.SharedKernel.Abstractions;
using DigiTrade.SharedKernel.Events;

namespace Settlement.Domain.Settlements;

public sealed class Settlement : IAggregateRoot<Guid>
{
    public Guid Id { get; internal set; }

    public Guid TradeId { get; internal set; }

    public string AccountId { get; internal set; } = string.Empty;

    public string CurrencyCode { get; internal set; } = string.Empty;

    public decimal NetAmount { get; internal set; }

    public SettlementStatus Status { get; internal set; }

    public DateTimeOffset InitiatedAtUtc { get; internal set; }

    public DateTimeOffset? FinalizedAtUtc { get; internal set; }

    public DateTimeOffset? FailedAtUtc { get; internal set; }

    public string? FailureReason { get; internal set; }

    public int Version { get; internal set; }

    public DateTimeOffset CreatedAt { get; internal set; }

    public DateTimeOffset UpdatedAt { get; internal set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents { get; internal set; } = [];

    public void ClearDomainEvents()
    {
        DomainEvents = [];
    }
}