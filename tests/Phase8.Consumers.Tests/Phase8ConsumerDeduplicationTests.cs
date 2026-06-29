using Audit.Persistence.Stores;
using BffNotificationService.Domain.Notifications;
using BffNotificationService.Infrastructure.Abstractions;
using BffNotificationService.Infrastructure.Consumers;
using BffNotificationService.Infrastructure.Events;
using BffNotificationService.Persistence.Stores;
using DigiTrade.Messaging.Persistence.Snapshots;
using Reporting.Infrastructure.Consumers;
using Reporting.Infrastructure.Events;
using Reporting.Persistence.Stores;
using Xunit;

namespace Phase8.Consumers.Tests;

public sealed class Phase8ConsumerDeduplicationTests
{
    [Fact]
    public async Task TerminalNotificationConsumerWhenDuplicateEventOnlyDeliversOnce()
    {
        var deliveryService = new TrackingNotificationClientDeliveryService();
        var deliveryStore = new InMemoryNotificationDeliveryStore();
        var snapshotStore = new InMemoryEventSnapshotStore();
        var consumer = new TerminalNotificationRequestedConsumer(
            deliveryService,
            deliveryStore,
            snapshotStore,
            TimeProvider.System);

        var integrationEvent = new TerminalNotificationRequestedEvent(
            Guid.NewGuid(),
            "notification.terminal_completion.requested",
            1,
            "trade-aggregate-1",
            DateTimeOffset.UtcNow,
            "user-1",
            "websocket",
            "Trade Completed",
            "Execution completed successfully.",
            "corr-001");

        await consumer.ConsumeAsync(integrationEvent);
        await consumer.ConsumeAsync(integrationEvent);

        Assert.Equal(1, deliveryService.DeliveryCount);
        var persistedDelivery = await deliveryStore.GetByEventIdAsync(integrationEvent.EventId);
        Assert.NotNull(persistedDelivery);
        Assert.Equal(integrationEvent.EventId, persistedDelivery.EventId);
    }

    [Fact]
    public async Task ReportingProjectionConsumerWhenDuplicateEventKeepsProjectionConsistent()
    {
        var projectionStore = new InMemoryTradeExecutionReportProjectionStore();
        var snapshotStore = new InMemoryEventSnapshotStore();
        var consumer = new TradeExecutionCompletedConsumer(
            projectionStore,
            snapshotStore,
            TimeProvider.System);

        var integrationEvent = new TradeExecutionCompletedEvent(
            Guid.NewGuid(),
            "trade.execution.completed",
            1,
            "trade-aggregate-42",
            DateTimeOffset.UtcNow,
            "process-42",
            "completed",
            3.5m,
            101.25m,
            "corr-042");

        await consumer.ConsumeAsync(integrationEvent);
        await consumer.ConsumeAsync(integrationEvent);

        var projection = await projectionStore.GetByAggregateIdAsync(integrationEvent.AggregateId);
        Assert.NotNull(projection);
        Assert.Equal(1, projection.Version);
        Assert.Equal(3.5m * 101.25m, projection.Notional);
        Assert.Equal("completed", projection.CompletionStatus);
    }

    [Fact]
    public async Task InMemoryAuditStoreWhenDuplicateRecordAppendsOnlyOnce()
    {
        var store = new InMemoryAuditEvidenceRecordStore();
        var eventId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var firstRecord = Audit.Domain.Evidence.AuditEvidenceRecordDomainService.Create(
            eventId,
            "business.process.completed",
            1,
            "aggregate-a",
            "process-a",
            "completed",
            "corr-a",
            now,
            "{\"result\":\"ok\"}",
            now);

        var duplicateRecord = Audit.Domain.Evidence.AuditEvidenceRecordDomainService.Create(
            eventId,
            "business.process.completed",
            1,
            "aggregate-a",
            "process-a",
            "completed",
            "corr-a",
            now,
            "{\"result\":\"ok\"}",
            now);

        var firstAppend = await store.AppendIfNotExistsAsync(firstRecord);
        var secondAppend = await store.AppendIfNotExistsAsync(duplicateRecord);

        Assert.True(firstAppend);
        Assert.False(secondAppend);
        Assert.True(await store.ExistsByEventIdAsync(eventId));
    }

    private sealed class TrackingNotificationClientDeliveryService : INotificationClientDeliveryService
    {
        public int DeliveryCount { get; private set; }

        public Task<NotificationDeliveryOutcome> DeliverAsync(
            TerminalNotificationRequestedEvent integrationEvent,
            CancellationToken cancellationToken = default)
        {
            DeliveryCount += 1;
            return Task.FromResult(new NotificationDeliveryOutcome(
                "in-memory",
                "delivered",
                DateTimeOffset.UtcNow));
        }
    }
}
