using DigiTrade.Messaging.Persistence.Outbox;

namespace Order.Persistence.Outbox;

public sealed class OrderOutboxMessageEntity
{
    public Guid Id { get; set; }

    public Guid EventId { get; set; }

    public string EventName { get; set; } = string.Empty;

    public string AggregateId { get; set; } = string.Empty;

    public string PartitionKey { get; set; } = string.Empty;

    public int EventVersion { get; set; }

    public DateTimeOffset OccurredAtUtc { get; set; }

    public string Payload { get; set; } = string.Empty;

    public string? HeadersJson { get; set; }

    public Guid? TransactionId { get; set; }

    public OutboxMessageStatus Status { get; set; }

    public int AttemptCount { get; set; }

    public DateTimeOffset? LastAttemptAtUtc { get; set; }

    public DateTimeOffset? PublishedAtUtc { get; set; }

    public string? FailureReason { get; set; }
}