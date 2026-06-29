using DigiTrade.Messaging.Contracts;

namespace Order.Application.Events;

public sealed record OrderRejectedIntegrationEvent(
    Guid EventId,
    string AggregateId,
    DateTimeOffset OccurredAtUtc) : IIntegrationEvent
{
    public const string IntegrationEventName = "order.rejected";

    public string EventName => IntegrationEventName;

    public int EventVersion => 1;
}