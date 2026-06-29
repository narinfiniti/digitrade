namespace DigiTrade.Common.Projections;

public interface ILocalDomainEventProjectionStore
{
    Task RecordAsync(
        string eventName,
        string aggregateId,
        Guid eventId,
        DateTimeOffset occurredAtUtc,
        object payload,
        CancellationToken cancellationToken = default);

    IReadOnlyCollection<LocalDomainEventProjectionRecord> Snapshot();
}