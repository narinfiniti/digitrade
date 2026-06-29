using DigiTrade.Common.Projections;
using MediatR;
using Risk.Domain.Margins.Events;

namespace Risk.Application.EventHandlers;

public sealed class MarginAccountOpenedDomainEventProjectionHandler(ILocalDomainEventProjectionStore projectionStore)
    : INotificationHandler<MarginAccountOpenedDomainEvent>
{
    public Task Handle(MarginAccountOpenedDomainEvent notification, CancellationToken cancellationToken)
    {
        return projectionStore.RecordAsync(
            nameof(MarginAccountOpenedDomainEvent),
            notification.MarginAccountId.ToString(),
            notification.EventId,
            notification.OccurredAtUtc,
            notification,
            cancellationToken);
    }
}