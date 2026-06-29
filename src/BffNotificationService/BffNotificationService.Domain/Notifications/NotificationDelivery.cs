using DigiTrade.SharedKernel.Abstractions;
using DigiTrade.SharedKernel.Events;

namespace BffNotificationService.Domain.Notifications;

public sealed class NotificationDelivery : IAggregateRoot<Guid>
{
    public Guid Id { get; internal set; }

    public Guid EventId { get; internal set; }

    public string AggregateId { get; internal set; } = string.Empty;

    public string RecipientId { get; internal set; } = string.Empty;

    public string Channel { get; internal set; } = string.Empty;

    public string Subject { get; internal set; } = string.Empty;

    public string Message { get; internal set; } = string.Empty;

    public string CorrelationId { get; internal set; } = string.Empty;

    public string DeliveryProvider { get; internal set; } = string.Empty;

    public string DeliveryStatus { get; internal set; } = string.Empty;

    public DateTimeOffset DeliveredAtUtc { get; internal set; }

    public int Version { get; internal set; }

    public DateTimeOffset CreatedAt { get; internal set; }

    public DateTimeOffset UpdatedAt { get; internal set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents { get; internal set; } = Array.Empty<IDomainEvent>();

    public void ClearDomainEvents()
    {
        DomainEvents = Array.Empty<IDomainEvent>();
    }
}