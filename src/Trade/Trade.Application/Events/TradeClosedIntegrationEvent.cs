using DigiTrade.Messaging.Contracts;

namespace Trade.Application.Events;

public sealed record TradeClosedIntegrationEvent(
    Guid EventId,
    string AggregateId,
    decimal ClosePrice,
    DateTimeOffset OccurredAtUtc) : IIntegrationEvent
{
    public const string IntegrationEventName = "trade.closed";

    public string EventName => IntegrationEventName;

    public int EventVersion => 1;
}