namespace BffNotificationService.Domain.Notifications;

public sealed record NotificationDeliveryOutcome(
    string Provider,
    string Status,
    DateTimeOffset DeliveredAtUtc);