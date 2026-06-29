namespace Audit.Domain.Evidence;

public static class AuditEvidenceRecordDomainService
{
    public static AuditEvidenceRecord Create(
        Guid eventId,
        string eventName,
        int eventVersion,
        string aggregateId,
        string businessProcessId,
        string status,
        string correlationId,
        DateTimeOffset occurredAtUtc,
        string payloadJson,
        DateTimeOffset createdAtUtc)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(eventId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);
        ArgumentException.ThrowIfNullOrWhiteSpace(businessProcessId);
        ArgumentException.ThrowIfNullOrWhiteSpace(status);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadJson);

        return new AuditEvidenceRecord
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            EventName = eventName,
            EventVersion = eventVersion,
            AggregateId = aggregateId,
            BusinessProcessId = businessProcessId,
            Status = status,
            CorrelationId = correlationId,
            OccurredAtUtc = occurredAtUtc,
            PayloadJson = payloadJson,
            Version = 1,
            CreatedAt = createdAtUtc,
            UpdatedAt = createdAtUtc,
        };
    }
}
