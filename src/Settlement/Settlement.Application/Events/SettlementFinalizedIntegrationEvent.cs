using DigiTrade.Messaging.Contracts;

namespace Settlement.Application.Events;

public sealed record SettlementFinalizedIntegrationEvent(
    Guid EventId,
    string AggregateId,
    string TradeId,
    string AccountId,
    string CurrencyCode,
    decimal NetAmount,
    DateTimeOffset OccurredAtUtc) : IIntegrationEvent
{
    public const string IntegrationEventName = "settlement.finalized";

    public string EventName => IntegrationEventName;

    public int EventVersion => 1;
}