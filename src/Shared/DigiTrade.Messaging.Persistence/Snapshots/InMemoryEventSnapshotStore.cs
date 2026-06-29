using System.Collections.Concurrent;

namespace DigiTrade.Messaging.Persistence.Snapshots;

public sealed class InMemoryEventSnapshotStore : IEventSnapshotStore
{
    private readonly ConcurrentDictionary<string, byte> snapshotKeys = new(StringComparer.Ordinal);

    public Task<bool> ExistsAsync(Guid eventId, string consumerName, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(eventId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerName);

        return Task.FromResult(snapshotKeys.ContainsKey(BuildKey(eventId, consumerName)));
    }

    public Task StoreAsync(EventSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        snapshotKeys.TryAdd(BuildKey(snapshot.EventId, snapshot.ConsumerName), 0);
        return Task.CompletedTask;
    }

    private static string BuildKey(Guid eventId, string consumerName)
    {
        return string.Concat(eventId.ToString("D"), ":", consumerName);
    }
}
