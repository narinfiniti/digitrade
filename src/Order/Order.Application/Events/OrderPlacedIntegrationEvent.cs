using DigiTrade.Messaging.Contracts;
using Order.Domain.Orders;

namespace Order.Application.Events;

public sealed record OrderPlacedIntegrationEvent(
    Guid EventId,
    string AggregateId,
    string AccountId,
    string InstrumentId,
    OrderDirection Direction,
    decimal Quantity,
    decimal RequestedPrice,
    DateTimeOffset OccurredAtUtc) : IIntegrationEvent
{
    public const string IntegrationEventName = "order.placed";

    public string EventName => IntegrationEventName;

    public int EventVersion => 1;
}