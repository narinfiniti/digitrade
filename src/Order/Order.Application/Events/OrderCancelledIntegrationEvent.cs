using DigiTrade.Messaging.Contracts;

namespace Order.Application.Events;

public sealed record OrderCancelledIntegrationEvent(
    Guid EventId,
    string AggregateId,
    DateTimeOffset OccurredAtUtc) : IIntegrationEvent
{
    public const string IntegrationEventName = "order.cancelled";

    public string EventName => IntegrationEventName;

    public int EventVersion => 1;
}