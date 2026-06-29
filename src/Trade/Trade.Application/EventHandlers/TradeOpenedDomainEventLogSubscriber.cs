using DigiTrade.Common.Projections;
using MediatR;
using Trade.Domain.Trades.Events;

namespace Trade.Application.EventHandlers;

public sealed class TradeOpenedDomainEventProjectionHandler(ILocalDomainEventProjectionStore projectionStore)
    : INotificationHandler<TradeOpenedDomainEvent>
{
    public Task Handle(TradeOpenedDomainEvent notification, CancellationToken cancellationToken)
    {
        return projectionStore.RecordAsync(
            nameof(TradeOpenedDomainEvent),
            notification.TradeId.ToString(),
            notification.EventId,
            notification.OccurredAtUtc,
            notification,
            cancellationToken);
    }
}