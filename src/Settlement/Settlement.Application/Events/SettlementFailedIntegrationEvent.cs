using DigiTrade.Messaging.Contracts;

namespace Settlement.Application.Events;

public sealed record SettlementFailedIntegrationEvent(
    Guid EventId,
    string AggregateId,
    string TradeId,
    string FailureReason,
    DateTimeOffset OccurredAtUtc) : IIntegrationEvent
{
    public const string IntegrationEventName = "settlement.failed";

    public string EventName => IntegrationEventName;

    public int EventVersion => 1;
}