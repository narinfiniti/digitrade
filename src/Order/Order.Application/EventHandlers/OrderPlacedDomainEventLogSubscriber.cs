using DigiTrade.Common.Projections;
using MediatR;
using Order.Domain.Orders.Events;

namespace Order.Application.EventHandlers;

public sealed class OrderPlacedDomainEventProjectionHandler(ILocalDomainEventProjectionStore projectionStore)
    : INotificationHandler<OrderPlacedDomainEvent>
{
    public Task Handle(OrderPlacedDomainEvent notification, CancellationToken cancellationToken)
    {
        return projectionStore.RecordAsync(
            nameof(OrderPlacedDomainEvent),
            notification.OrderId.ToString(),
            notification.EventId,
            notification.OccurredAtUtc,
            notification,
            cancellationToken);
    }
}