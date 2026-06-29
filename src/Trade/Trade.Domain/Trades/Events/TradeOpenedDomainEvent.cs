using DigiTrade.SharedKernel.Events;

namespace Trade.Domain.Trades.Events;

public sealed record TradeOpenedDomainEvent(
    Guid EventId,
    Guid TradeId,
    string AccountId,
    string InstrumentId,
    TradeDirection Direction,
    decimal Quantity,
    decimal OpenPrice,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;