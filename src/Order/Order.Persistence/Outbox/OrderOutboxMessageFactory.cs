using System.Text.Json;
using DigiTrade.Messaging.Persistence.Outbox;
using DigiTrade.SharedKernel.Events;
using Order.Application.Events;
using Order.Domain.Orders.Events;

namespace Order.Persistence.Outbox;

internal static class OrderOutboxMessageFactory
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static OutboxMessage Create(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        return domainEvent switch
        {
            OrderPlacedDomainEvent orderPlaced => CreateOrderPlacedMessage(orderPlaced),
            OrderAcceptedDomainEvent orderAccepted => CreateOrderAcceptedMessage(orderAccepted),
            OrderRejectedDomainEvent orderRejected => CreateOrderRejectedMessage(orderRejected),
            OrderCancelledDomainEvent orderCancelled => CreateOrderCancelledMessage(orderCancelled),
            _ => throw new InvalidOperationException($"Order outbox does not support domain event '{domainEvent.GetType().Name}'."),
        };
    }

    private static OutboxMessage CreateOrderPlacedMessage(OrderPlacedDomainEvent domainEvent)
    {
        var integrationEvent = new OrderPlacedIntegrationEvent(
            domainEvent.EventId,
            domainEvent.OrderId.ToString("D"),
            domainEvent.AccountId,
            domainEvent.InstrumentId,
            domainEvent.Direction,
            domainEvent.Quantity,
            domainEvent.RequestedPrice,
            domainEvent.OccurredAtUtc);

        return CreateMessage(integrationEvent);
    }

    private static OutboxMessage CreateOrderAcceptedMessage(OrderAcceptedDomainEvent domainEvent)
    {
        var integrationEvent = new OrderAcceptedIntegrationEvent(
            domainEvent.EventId,
            domainEvent.OrderId.ToString("D"),
            domainEvent.OccurredAtUtc);

        return CreateMessage(integrationEvent);
    }

    private static OutboxMessage CreateOrderRejectedMessage(OrderRejectedDomainEvent domainEvent)
    {
        var integrationEvent = new OrderRejectedIntegrationEvent(
            domainEvent.EventId,
            domainEvent.OrderId.ToString("D"),
            domainEvent.OccurredAtUtc);

        return CreateMessage(integrationEvent);
    }

    private static OutboxMessage CreateOrderCancelledMessage(OrderCancelledDomainEvent domainEvent)
    {
        var integrationEvent = new OrderCancelledIntegrationEvent(
            domainEvent.EventId,
            domainEvent.OrderId.ToString("D"),
            domainEvent.OccurredAtUtc);

        return CreateMessage(integrationEvent);
    }

    private static OutboxMessage CreateMessage<TIntegrationEvent>(TIntegrationEvent integrationEvent)
        where TIntegrationEvent : class, DigiTrade.Messaging.Contracts.IIntegrationEvent
    {
        return new OutboxMessage(
            Guid.NewGuid(),
            integrationEvent.EventId,
            integrationEvent.EventName,
            integrationEvent.AggregateId,
            integrationEvent.AggregateId,
            integrationEvent.EventVersion,
            integrationEvent.OccurredAtUtc,
            JsonSerializer.Serialize(integrationEvent, SerializerOptions),
            null,
            null,
            OutboxMessageStatus.Pending,
            0,
            null,
            null,
            null);
    }
}