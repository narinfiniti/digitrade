using DigiTrade.Messaging.Contracts;
using Trade.Domain.Trades;

namespace Trade.Application.Events;

public sealed record TradeOpenedIntegrationEvent(
    Guid EventId,
    string AggregateId,
    string AccountId,
    string InstrumentId,
    TradeDirection Direction,
    decimal Quantity,
    decimal OpenPrice,
    DateTimeOffset OccurredAtUtc) : IIntegrationEvent
{
    public const string IntegrationEventName = "trade.opened";

    public string EventName => IntegrationEventName;

    public int EventVersion => 1;
}