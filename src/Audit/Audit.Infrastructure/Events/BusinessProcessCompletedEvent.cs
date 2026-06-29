using DigiTrade.Messaging.Contracts;

namespace Audit.Infrastructure.Events;

public sealed record BusinessProcessCompletedEvent(
    Guid EventId,
    string EventName,
    int EventVersion,
    string AggregateId,
    DateTimeOffset OccurredAtUtc,
    string BusinessProcessId,
    string Status,
    string CorrelationId,
    string PayloadJson) : IIntegrationEvent;
