using DigiTrade.SharedKernel.Abstractions;
using DigiTrade.SharedKernel.Events;

namespace Order.Domain.Orders;

public sealed class Order : IAggregateRoot<Guid>
{
    public Guid Id { get; internal set; }

    public string AccountId { get; internal set; } = string.Empty;

    public string InstrumentId { get; internal set; } = string.Empty;

    public OrderDirection Direction { get; internal set; }

    public decimal Quantity { get; internal set; }

    public decimal RequestedPrice { get; internal set; }

    public OrderStatus Status { get; internal set; }

    public DateTimeOffset SubmittedAtUtc { get; internal set; }

    public DateTimeOffset? AcceptedAtUtc { get; internal set; }

    public DateTimeOffset? RejectedAtUtc { get; internal set; }

    public DateTimeOffset? CancelledAtUtc { get; internal set; }

    public int Version { get; internal set; }

    public DateTimeOffset CreatedAt { get; internal set; }

    public DateTimeOffset UpdatedAt { get; internal set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents { get; internal set; } = [];

    public void ClearDomainEvents()
    {
        DomainEvents = [];
    }
}