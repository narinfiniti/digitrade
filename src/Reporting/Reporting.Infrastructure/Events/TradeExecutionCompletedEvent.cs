using DigiTrade.Messaging.Contracts;

namespace Reporting.Infrastructure.Events;

public sealed record TradeExecutionCompletedEvent(
    Guid EventId,
    string EventName,
    int EventVersion,
    string AggregateId,
    DateTimeOffset OccurredAtUtc,
    string BusinessProcessId,
    string CompletionStatus,
    decimal FilledQuantity,
    decimal AveragePrice,
    string CorrelationId) : IIntegrationEvent;