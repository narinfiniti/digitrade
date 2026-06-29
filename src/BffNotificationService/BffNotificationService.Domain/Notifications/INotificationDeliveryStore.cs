namespace BffNotificationService.Domain.Notifications;

public interface INotificationDeliveryStore
{
    Task AddAsync(NotificationDelivery notificationDelivery, CancellationToken cancellationToken = default);

    Task<NotificationDelivery?> GetAsync(Guid notificationDeliveryId, CancellationToken cancellationToken = default);

    Task<NotificationDelivery?> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);
}