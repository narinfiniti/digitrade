using DigiTrade.Messaging.Contracts;

namespace Risk.Application.Events;

public sealed record MarginReleasedIntegrationEvent(
    Guid EventId,
    string AggregateId,
    decimal Amount,
    decimal ReservedMargin,
    DateTimeOffset OccurredAtUtc) : IIntegrationEvent
{
    public const string IntegrationEventName = "risk.margin.released";

    public string EventName => IntegrationEventName;

    public int EventVersion => 1;
}