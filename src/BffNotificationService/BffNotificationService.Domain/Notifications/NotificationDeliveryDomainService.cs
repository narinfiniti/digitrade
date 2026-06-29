namespace BffNotificationService.Domain.Notifications;

public static class NotificationDeliveryDomainService
{
    public static NotificationDelivery Create(
        Guid eventId,
        string aggregateId,
        string recipientId,
        string channel,
        string subject,
        string message,
        string correlationId,
        NotificationDeliveryOutcome deliveryOutcome,
        DateTimeOffset createdAtUtc)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(eventId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);
        ArgumentException.ThrowIfNullOrWhiteSpace(recipientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(channel);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        ArgumentNullException.ThrowIfNull(deliveryOutcome);

        return new NotificationDelivery
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            AggregateId = aggregateId,
            RecipientId = recipientId,
            Channel = channel,
            Subject = subject,
            Message = message,
            CorrelationId = correlationId,
            DeliveryProvider = deliveryOutcome.Provider,
            DeliveryStatus = deliveryOutcome.Status,
            DeliveredAtUtc = deliveryOutcome.DeliveredAtUtc,
            Version = 1,
            CreatedAt = createdAtUtc,
            UpdatedAt = createdAtUtc,
        };
    }
}