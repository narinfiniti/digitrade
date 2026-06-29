using DigiTrade.Common.Projections;
using MediatR;
using Settlement.Domain.Settlements.Events;

namespace Settlement.Application.EventHandlers;

public sealed class SettlementInitiatedDomainEventProjectionHandler(ILocalDomainEventProjectionStore projectionStore)
    : INotificationHandler<SettlementInitiatedDomainEvent>
{
    public Task Handle(SettlementInitiatedDomainEvent notification, CancellationToken cancellationToken)
    {
        return projectionStore.RecordAsync(
            nameof(SettlementInitiatedDomainEvent),
            notification.SettlementId.ToString(),
            notification.EventId,
            notification.OccurredAtUtc,
            notification,
            cancellationToken);
    }
}