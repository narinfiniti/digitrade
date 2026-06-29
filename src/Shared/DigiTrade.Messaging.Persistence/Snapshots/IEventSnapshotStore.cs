namespace DigiTrade.Messaging.Persistence.Snapshots;

public interface IEventSnapshotStore
{
    Task<bool> ExistsAsync(Guid eventId, string consumerName, CancellationToken cancellationToken = default);

    Task StoreAsync(EventSnapshot snapshot, CancellationToken cancellationToken = default);
}