using DigiTrade.SharedKernel.Abstractions;
using DigiTrade.SharedKernel.Events;

namespace Trade.Domain.Trades;

public sealed class Trade : IAggregateRoot<Guid>
{
    public Guid Id { get; internal set; }

    public string AccountId { get; internal set; } = string.Empty;

    public string InstrumentId { get; internal set; } = string.Empty;

    public TradeDirection Direction { get; internal set; }

    public TradeStatus Status { get; internal set; }

    public decimal Quantity { get; internal set; }

    public decimal OpenPrice { get; internal set; }

    public DateTimeOffset OpenedAtUtc { get; internal set; }

    public decimal? ClosePrice { get; internal set; }

    public DateTimeOffset? ClosedAtUtc { get; internal set; }

    public int Version { get; internal set; }

    public DateTimeOffset CreatedAt { get; internal set; }

    public DateTimeOffset UpdatedAt { get; internal set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents { get; internal set; } = [];

    public void ClearDomainEvents()
    {
        DomainEvents = [];
    }
}