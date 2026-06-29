using DigiTrade.Messaging.Persistence.Outbox;
using DigiTrade.SharedKernel.Events;
using Microsoft.EntityFrameworkCore;
using Settlement.Application.Abstractions;

namespace Settlement.Persistence.Outbox;

public sealed class SettlementOutboxStore(SettlementDbContext dbContext) : ISettlementOutboxWriter, IOutboxStore
{
    public async Task EnqueueAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);

        if (domainEvents.Count == 0)
        {
            return;
        }

        var entities = domainEvents
            .Select(SettlementOutboxMessageFactory.Create)
            .Select(ToEntity)
            .ToArray();

        await dbContext.OutboxMessages.AddRangeAsync(entities, cancellationToken);
    }

    public Task EnqueueAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        dbContext.OutboxMessages.Add(ToEntity(message));
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyCollection<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var normalizedBatchSize = Math.Max(batchSize, 1);

        var messages = await dbContext.OutboxMessages
            .AsNoTracking()
            .Where(message => message.Status == OutboxMessageStatus.Pending)
            .ToListAsync(cancellationToken);

        return messages
            .OrderBy(message => message.OccurredAtUtc)
            .ThenBy(message => message.Id)
            .Take(normalizedBatchSize)
            .Select(ToModel)
            .ToArray();
    }

    public async Task MarkPublishedAsync(Guid messageId, DateTimeOffset publishedAtUtc, CancellationToken cancellationToken = default)
    {
        var message = await dbContext.OutboxMessages
            .SingleAsync(outboxMessage => outboxMessage.Id == messageId, cancellationToken);

        message.Status = OutboxMessageStatus.Published;
        message.AttemptCount += 1;
        message.LastAttemptAtUtc = publishedAtUtc;
        message.PublishedAtUtc = publishedAtUtc;
        message.FailureReason = null;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(Guid messageId, string failureReason, DateTimeOffset failedAtUtc, CancellationToken cancellationToken = default)
    {
        var message = await dbContext.OutboxMessages
            .SingleAsync(outboxMessage => outboxMessage.Id == messageId, cancellationToken);

        message.Status = OutboxMessageStatus.Failed;
        message.AttemptCount += 1;
        message.LastAttemptAtUtc = failedAtUtc;
        message.FailureReason = string.IsNullOrWhiteSpace(failureReason)
            ? "Settlement outbox publish failed."
            : failureReason;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static SettlementOutboxMessageEntity ToEntity(OutboxMessage message)
    {
        return new SettlementOutboxMessageEntity
        {
            Id = message.MessageId,
            EventId = message.EventId,
            EventName = message.EventName,
            AggregateId = message.AggregateId,
            PartitionKey = message.PartitionKey,
            EventVersion = message.EventVersion,
            OccurredAtUtc = message.OccurredAtUtc,
            Payload = message.Payload,
            HeadersJson = message.HeadersJson,
            TransactionId = message.TransactionId,
            Status = message.Status,
            AttemptCount = message.AttemptCount,
            LastAttemptAtUtc = message.LastAttemptAtUtc,
            PublishedAtUtc = message.PublishedAtUtc,
            FailureReason = message.FailureReason,
        };
    }

    private static OutboxMessage ToModel(SettlementOutboxMessageEntity message)
    {
        return new OutboxMessage(
            message.Id,
            message.EventId,
            message.EventName,
            message.AggregateId,
            message.PartitionKey,
            message.EventVersion,
            message.OccurredAtUtc,
            message.Payload,
            message.HeadersJson,
            message.TransactionId,
            message.Status,
            message.AttemptCount,
            message.LastAttemptAtUtc,
            message.PublishedAtUtc,
            message.FailureReason);
    }
}