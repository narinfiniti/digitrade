using DigiTrade.Messaging.Contracts;

namespace BffNotificationService.Infrastructure.Events;

public sealed record TerminalNotificationRequestedEvent(
    Guid EventId,
    string EventName,
    int EventVersion,
    string AggregateId,
    DateTimeOffset OccurredAtUtc,
    string RecipientId,
    string Channel,
    string Subject,
    string Message,
    string CorrelationId) : IIntegrationEvent;