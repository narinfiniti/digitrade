using BffNotificationService.Domain.Notifications;
using BffNotificationService.Infrastructure.Abstractions;
using BffNotificationService.Infrastructure.Events;
using DigiTrade.Messaging.Contracts;
using DigiTrade.Messaging.Persistence.Snapshots;

namespace BffNotificationService.Infrastructure.Consumers;

public sealed class TerminalNotificationRequestedConsumer(
    INotificationClientDeliveryService notificationClientDeliveryService,
    INotificationDeliveryStore notificationDeliveryStore,
    IEventSnapshotStore eventSnapshotStore,
    TimeProvider timeProvider) : IIntegrationEventConsumer<TerminalNotificationRequestedEvent>
{
    private const string ConsumerName = "bff-notification.terminal-notification-requested";

    public async Task ConsumeAsync(TerminalNotificationRequestedEvent integrationEvent, CancellationToken cancellationToken = default)
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

        var deliveryOutcome = await notificationClientDeliveryService.DeliverAsync(integrationEvent, cancellationToken);
        var notificationDelivery = NotificationDeliveryDomainService.Create(
            integrationEvent.EventId,
            integrationEvent.AggregateId,
            integrationEvent.RecipientId,
            integrationEvent.Channel,
            integrationEvent.Subject,
            integrationEvent.Message,
            integrationEvent.CorrelationId,
            deliveryOutcome,
            timeProvider.GetUtcNow());

        await notificationDeliveryStore.AddAsync(notificationDelivery, cancellationToken);
    }
}