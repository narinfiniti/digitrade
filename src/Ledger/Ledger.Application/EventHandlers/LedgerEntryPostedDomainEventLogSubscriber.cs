using DigiTrade.Common.Projections;
using Ledger.Domain.Ledgers;
using MediatR;

namespace Ledger.Application.EventHandlers;

public sealed class LedgerEntryPostedDomainEventProjectionHandler(ILocalDomainEventProjectionStore projectionStore)
    : INotificationHandler<LedgerEntryPostedDomainEvent>
{
    public Task Handle(LedgerEntryPostedDomainEvent notification, CancellationToken cancellationToken)
    {
        return projectionStore.RecordAsync(
            nameof(LedgerEntryPostedDomainEvent),
            notification.LedgerEntryId.ToString(),
            notification.EventId,
            notification.OccurredAtUtc,
            notification,
            cancellationToken);
    }
}