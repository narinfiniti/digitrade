namespace Reporting.Domain.Projections;

public static class TradeExecutionReportProjectionDomainService
{
    public static TradeExecutionReportProjection CreateOrUpdate(
        TradeExecutionReportProjection? existingProjection,
        Guid eventId,
        string eventName,
        int eventVersion,
        string aggregateId,
        DateTimeOffset occurredAtUtc,
        string businessProcessId,
        string completionStatus,
        decimal filledQuantity,
        decimal averagePrice,
        string correlationId,
        DateTimeOffset updatedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);
        ArgumentException.ThrowIfNullOrWhiteSpace(businessProcessId);
        ArgumentException.ThrowIfNullOrWhiteSpace(completionStatus);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        var projection = existingProjection ?? CreateNew(
            eventId,
            eventName,
            eventVersion,
            aggregateId,
            occurredAtUtc,
            businessProcessId,
            completionStatus,
            filledQuantity,
            averagePrice,
            correlationId,
            updatedAtUtc);

        projection.AggregateId = aggregateId;
        projection.BusinessProcessId = businessProcessId;
        projection.CompletionStatus = completionStatus;
        projection.FilledQuantity = filledQuantity;
        projection.AveragePrice = averagePrice;
        projection.Notional = filledQuantity * averagePrice;
        projection.CorrelationId = correlationId;
        projection.LastEventId = eventId;
        projection.LastEventName = eventName;
        projection.LastEventVersion = eventVersion;
        projection.LastOccurredAtUtc = occurredAtUtc;
        projection.UpdatedAt = updatedAtUtc;

        if (existingProjection is not null)
        {
            projection.Version += 1;
        }

        return projection;
    }

    private static TradeExecutionReportProjection CreateNew(
        Guid eventId,
        string eventName,
        int eventVersion,
        string aggregateId,
        DateTimeOffset occurredAtUtc,
        string businessProcessId,
        string completionStatus,
        decimal filledQuantity,
        decimal averagePrice,
        string correlationId,
        DateTimeOffset createdAtUtc)
    {
        return new TradeExecutionReportProjection
        {
            Id = Guid.NewGuid(),
            AggregateId = aggregateId,
            BusinessProcessId = businessProcessId,
            CompletionStatus = completionStatus,
            FilledQuantity = filledQuantity,
            AveragePrice = averagePrice,
            Notional = filledQuantity * averagePrice,
            CorrelationId = correlationId,
            LastEventId = eventId,
            LastEventName = eventName,
            LastEventVersion = eventVersion,
            LastOccurredAtUtc = occurredAtUtc,
            Version = 1,
            CreatedAt = createdAtUtc,
            UpdatedAt = createdAtUtc,
        };
    }
}