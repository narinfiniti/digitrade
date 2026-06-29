using BffNotificationService.Domain.Notifications;
using BffNotificationService.Infrastructure.Abstractions;
using BffNotificationService.Infrastructure.Events;

namespace BffNotificationService.Infrastructure.Deliveries;

public sealed class InMemoryNotificationClientDeliveryService(
    TimeProvider timeProvider,
    INotificationWebSocketConnectionService notificationWebSocketConnectionService) : INotificationClientDeliveryService
{
    public async Task<NotificationDeliveryOutcome> DeliverAsync(
        TerminalNotificationRequestedEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var deliveredAtUtc = timeProvider.GetUtcNow();

        if (string.Equals(integrationEvent.Channel, "websocket", StringComparison.OrdinalIgnoreCase))
        {
            var deliveredConnectionCount = await notificationWebSocketConnectionService.BroadcastAsync(
                integrationEvent,
                deliveredAtUtc,
                cancellationToken);

            return new NotificationDeliveryOutcome(
                "InMemoryWebSocketClientNotifier",
                deliveredConnectionCount > 0 ? "Delivered" : "NoActiveConnections",
                deliveredAtUtc);
        }

        return new NotificationDeliveryOutcome(
            "InMemoryClientNotifier",
            "Delivered",
            deliveredAtUtc);
    }
}