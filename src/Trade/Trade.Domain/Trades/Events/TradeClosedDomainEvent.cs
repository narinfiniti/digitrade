using DigiTrade.SharedKernel.Events;

namespace Trade.Domain.Trades.Events;

public sealed record TradeClosedDomainEvent(
    Guid EventId,
    Guid TradeId,
    decimal ClosePrice,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;