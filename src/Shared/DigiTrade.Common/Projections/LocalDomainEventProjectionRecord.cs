namespace DigiTrade.Common.Projections;

public sealed record LocalDomainEventProjectionRecord(
    string EventName,
    string AggregateId,
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    string PayloadJson);