using DigiTrade.Messaging.Contracts;

namespace Settlement.Application.Events;

public sealed record SettlementInitiatedIntegrationEvent(
    Guid EventId,
    string AggregateId,
    string TradeId,
    string AccountId,
    string CurrencyCode,
    decimal NetAmount,
    DateTimeOffset OccurredAtUtc) : IIntegrationEvent
{
    public const string IntegrationEventName = "settlement.initiated";

    public string EventName => IntegrationEventName;

    public int EventVersion => 1;
}