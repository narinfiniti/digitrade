using DigiTrade.SharedKernel.Events;

namespace Order.Domain.Orders.Events;

public sealed record OrderRejectedDomainEvent(
    Guid EventId,
    Guid OrderId,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;