using System.Collections.Concurrent;
using BffNotificationService.Domain.Notifications;

namespace BffNotificationService.Persistence.Stores;

public sealed class InMemoryNotificationDeliveryStore : INotificationDeliveryStore
{
    private readonly ConcurrentDictionary<Guid, NotificationDelivery> notificationDeliveries = new();
    private readonly ConcurrentDictionary<Guid, Guid> deliveryIdsByEventId = new();

    public Task AddAsync(NotificationDelivery notificationDelivery, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notificationDelivery);

        notificationDeliveries[notificationDelivery.Id] = notificationDelivery;
        deliveryIdsByEventId[notificationDelivery.EventId] = notificationDelivery.Id;
        return Task.CompletedTask;
    }

    public Task<NotificationDelivery?> GetAsync(Guid notificationDeliveryId, CancellationToken cancellationToken = default)
    {
        notificationDeliveries.TryGetValue(notificationDeliveryId, out var notificationDelivery);
        return Task.FromResult(notificationDelivery);
    }

    public Task<NotificationDelivery?> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        if (deliveryIdsByEventId.TryGetValue(eventId, out var notificationDeliveryId))
        {
            notificationDeliveries.TryGetValue(notificationDeliveryId, out var notificationDelivery);
            return Task.FromResult(notificationDelivery);
        }

        return Task.FromResult<NotificationDelivery?>(null);
    }
}