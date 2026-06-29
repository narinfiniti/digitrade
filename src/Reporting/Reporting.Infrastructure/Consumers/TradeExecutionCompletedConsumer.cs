using DigiTrade.Messaging.Contracts;
using DigiTrade.Messaging.Persistence.Snapshots;
using Reporting.Domain.Projections;
using Reporting.Infrastructure.Events;

namespace Reporting.Infrastructure.Consumers;

public sealed class TradeExecutionCompletedConsumer(
    ITradeExecutionReportProjectionStore projectionStore,
    IEventSnapshotStore eventSnapshotStore,
    TimeProvider timeProvider) : IIntegrationEventConsumer<TradeExecutionCompletedEvent>
{
    private const string ConsumerName = "reporting.trade-execution-completed";

    public async Task ConsumeAsync(TradeExecutionCompletedEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var isDuplicate = await eventSnapshotStore.ExistsAsync(integrationEvent.EventId, ConsumerName, cancellationToken);
        if (isDuplicate)
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

        var existingProjection = await projectionStore.GetByAggregateIdAsync(integrationEvent.AggregateId, cancellationToken);
        var updatedProjection = TradeExecutionReportProjectionDomainService.CreateOrUpdate(
            existingProjection,
            integrationEvent.EventId,
            integrationEvent.EventName,
            integrationEvent.EventVersion,
            integrationEvent.AggregateId,
            integrationEvent.OccurredAtUtc,
            integrationEvent.BusinessProcessId,
            integrationEvent.CompletionStatus,
            integrationEvent.FilledQuantity,
            integrationEvent.AveragePrice,
            integrationEvent.CorrelationId,
            timeProvider.GetUtcNow());

        await projectionStore.UpsertAsync(updatedProjection, cancellationToken);
    }
}