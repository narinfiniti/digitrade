using DigiTrade.SharedKernel.Events;

namespace Order.Domain.Orders.Events;

public sealed record OrderPlacedDomainEvent(
    Guid EventId,
    Guid OrderId,
    string AccountId,
    string InstrumentId,
    OrderDirection Direction,
    decimal Quantity,
    decimal RequestedPrice,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;