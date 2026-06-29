namespace DigiTrade.Messaging.Persistence.Outbox;

public sealed record OutboxMessage(
    Guid MessageId,
    Guid EventId,
    string EventName,
    string AggregateId,
    string PartitionKey,
    int EventVersion,
    DateTimeOffset OccurredAtUtc,
    string Payload,
    string? HeadersJson,
    Guid? TransactionId,
    OutboxMessageStatus Status,
    int AttemptCount,
    DateTimeOffset? LastAttemptAtUtc,
    DateTimeOffset? PublishedAtUtc,
    string? FailureReason);