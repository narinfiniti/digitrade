using DigiTrade.Messaging.Contracts;

namespace Risk.Application.Events;

public sealed record MarginAccountOpenedIntegrationEvent(
    Guid EventId,
    string AggregateId,
    string AccountId,
    string CurrencyCode,
    decimal TotalMargin,
    DateTimeOffset OccurredAtUtc) : IIntegrationEvent
{
    public const string IntegrationEventName = "risk.margin-account.opened";

    public string EventName => IntegrationEventName;

    public int EventVersion => 1;
}