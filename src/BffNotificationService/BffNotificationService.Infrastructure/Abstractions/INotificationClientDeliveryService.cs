using BffNotificationService.Domain.Notifications;
using BffNotificationService.Infrastructure.Events;

namespace BffNotificationService.Infrastructure.Abstractions;

public interface INotificationClientDeliveryService
{
    Task<NotificationDeliveryOutcome> DeliverAsync(
        TerminalNotificationRequestedEvent integrationEvent,
        CancellationToken cancellationToken = default);
}