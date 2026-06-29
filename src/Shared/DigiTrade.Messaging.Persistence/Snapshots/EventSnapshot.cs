namespace DigiTrade.Messaging.Persistence.Snapshots;

public sealed record EventSnapshot(
    Guid EventId,
    string ConsumerName,
    string AggregateId,
    DateTimeOffset ProcessedAtUtc);