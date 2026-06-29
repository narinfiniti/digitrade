using Audit.Domain.Evidence;
using Audit.Infrastructure.Events;
using DigiTrade.Messaging.Contracts;
using DigiTrade.Messaging.Persistence.Snapshots;

namespace Audit.Infrastructure.Consumers;

public sealed class BusinessProcessCompletedAuditConsumer(
    IAuditEvidenceRecordStore auditEvidenceRecordStore,
    IEventSnapshotStore eventSnapshotStore,
    TimeProvider timeProvider) : IIntegrationEventConsumer<BusinessProcessCompletedEvent>
{
    private const string ConsumerName = "audit.business-process-completed";

    public async Task ConsumeAsync(BusinessProcessCompletedEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var isDuplicateSnapshot = await eventSnapshotStore.ExistsAsync(integrationEvent.EventId, ConsumerName, cancellationToken);
        if (isDuplicateSnapshot)
        {
            return;
        }

        await eventSnapshotStore.StoreAsync(
            new EventSnapshot(
                integrationEvent.EventId,
                ConsumerName,
                integrationEvent.AggregateId,
                timeProvider.GetUtcNow()),
            cancellationToken);

        var isDuplicateEvent = await auditEvidenceRecordStore.ExistsByEventIdAsync(
            integrationEvent.EventId,
            cancellationToken);
        if (isDuplicateEvent)
        {
            return;
        }

        var auditRecord = AuditEvidenceRecordDomainService.Create(
            integrationEvent.EventId,
            integrationEvent.EventName,
            integrationEvent.EventVersion,
            integrationEvent.AggregateId,
            integrationEvent.BusinessProcessId,
            integrationEvent.Status,
            integrationEvent.CorrelationId,
            integrationEvent.OccurredAtUtc,
            integrationEvent.PayloadJson,
            timeProvider.GetUtcNow());

        await auditEvidenceRecordStore.AppendIfNotExistsAsync(auditRecord, cancellationToken);
    }
}
