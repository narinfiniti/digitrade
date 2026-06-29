using System.Collections.Concurrent;
using System.Text.Json;

namespace DigiTrade.Common.Projections;

public sealed class InMemoryLocalDomainEventProjectionStore : ILocalDomainEventProjectionStore
{
    private readonly ConcurrentQueue<LocalDomainEventProjectionRecord> records = new();

    public Task RecordAsync(
        string eventName,
        string aggregateId,
        Guid eventId,
        DateTimeOffset occurredAtUtc,
        object payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);
        ArgumentNullException.ThrowIfNull(payload);

        cancellationToken.ThrowIfCancellationRequested();

        records.Enqueue(new LocalDomainEventProjectionRecord(
            eventName,
            aggregateId,
            eventId,
            occurredAtUtc,
            JsonSerializer.Serialize(payload, payload.GetType())));

        return Task.CompletedTask;
    }

    public IReadOnlyCollection<LocalDomainEventProjectionRecord> Snapshot()
    {
        return records.ToArray();
    }
}