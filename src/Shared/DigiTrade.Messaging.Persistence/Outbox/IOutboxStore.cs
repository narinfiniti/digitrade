namespace DigiTrade.Messaging.Persistence.Outbox;

public interface IOutboxStore
{
    Task EnqueueAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken = default);

    Task MarkPublishedAsync(Guid messageId, DateTimeOffset publishedAtUtc, CancellationToken cancellationToken = default);

    Task MarkFailedAsync(Guid messageId, string failureReason, DateTimeOffset failedAtUtc, CancellationToken cancellationToken = default);
}