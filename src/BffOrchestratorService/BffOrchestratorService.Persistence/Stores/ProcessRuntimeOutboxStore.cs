using System.Data;
using BffOrchestratorService.Domain;
using BffOrchestratorService.Domain.Abstractions;
using BffOrchestratorService.Domain.Entities;
using BffOrchestratorService.Domain.Options;
using DigiTrade.Messaging.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace BffOrchestratorService.Persistence.Stores;

public sealed class ProcessRuntimeOutboxStore : IProcessRuntimeOutboxStore
{
    private readonly BffOrchestratorDbContext dbContext;
    private readonly TimeProvider timeProvider;
    private readonly int maxPublishAttempts;
    private readonly double baseRetryDelayMilliseconds;
    private readonly double maxRetryDelayMilliseconds;
    private readonly double processingLeaseTimeoutMilliseconds;

    public ProcessRuntimeOutboxStore(
        BffOrchestratorDbContext dbContext,
        TimeProvider timeProvider,
        IOptions<ProcessRuntimeOutboxStoreOptions> options)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(options);

        this.dbContext = dbContext;
        this.timeProvider = timeProvider;

        var storeOptions = options.Value;
        maxPublishAttempts = Math.Max(storeOptions.MaxPublishAttempts, 1);
        baseRetryDelayMilliseconds = Math.Max(storeOptions.BaseRetryDelay.TotalMilliseconds, 1d);
        maxRetryDelayMilliseconds = Math.Max(storeOptions.MaxRetryDelay.TotalMilliseconds, 1d);
        processingLeaseTimeoutMilliseconds = Math.Max(storeOptions.ProcessingLeaseTimeout.TotalMilliseconds, 1d);
    }

    public Task EnqueueAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        dbContext.ProcessRuntimeOutboxMessages.Add(ToEntity(message));
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyCollection<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        var normalizedBatchSize = Math.Max(batchSize, 1);
        var asOfUtc = timeProvider.GetUtcNow();
        var claimedMessages = new List<OutboxMessage>(normalizedBatchSize);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var connection = dbContext.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = """
            with eligible_messages as (
                select om."Id"
                from outbox_messages om
                where om."AttemptCount" < @max_publish_attempts
                  and (
                    om."Status" = @pending_status
                    or (
                        om."Status" = @failed_status
                        and (
                            om."LastAttemptAtUtc" is null
                            or om."LastAttemptAtUtc" + (least(
                                power(2.0, greatest(om."AttemptCount", 1) - 1) * @base_retry_delay_ms,
                                @max_retry_delay_ms) * interval '1 millisecond') <= @as_of_utc)
                    )
                    or (
                        om."Status" = @processing_status
                        and (
                            om."LastAttemptAtUtc" is null
                            or om."LastAttemptAtUtc" + (@processing_lease_timeout_ms * interval '1 millisecond') <= @as_of_utc)
                    )
                  )
                order by om."OccurredAtUtc", om."Id"
                for update skip locked
                limit @batch_size
            ),
            claimed_messages as (
                update outbox_messages om
                set "Status" = @processing_status,
                    "AttemptCount" = om."AttemptCount" + 1,
                    "LastAttemptAtUtc" = @as_of_utc,
                    "FailureReason" = null
                from eligible_messages eligible
                where om."Id" = eligible."Id"
                returning
                    om."Id",
                    om."EventId",
                    om."EventName",
                    om."AggregateId",
                    om."PartitionKey",
                    om."EventVersion",
                    om."OccurredAtUtc",
                    om."Payload",
                    om."HeadersJson",
                    om."TransactionId",
                    om."Status",
                    om."AttemptCount",
                    om."LastAttemptAtUtc",
                    om."PublishedAtUtc",
                    om."FailureReason"
            )
            select
                claimed."Id",
                claimed."EventId",
                claimed."EventName",
                claimed."AggregateId",
                claimed."PartitionKey",
                claimed."EventVersion",
                claimed."OccurredAtUtc",
                claimed."Payload",
                claimed."HeadersJson",
                claimed."TransactionId",
                claimed."Status",
                claimed."AttemptCount",
                claimed."LastAttemptAtUtc",
                claimed."PublishedAtUtc",
                claimed."FailureReason"
            from claimed_messages claimed
            order by claimed."OccurredAtUtc", claimed."Id";
            """;

        AddCommandParameter(command, "batch_size", normalizedBatchSize);
        AddCommandParameter(command, "as_of_utc", asOfUtc);
        AddCommandParameter(command, "pending_status", (int)OutboxMessageStatus.Pending);
        AddCommandParameter(command, "failed_status", (int)OutboxMessageStatus.Failed);
        AddCommandParameter(command, "processing_status", (int)OutboxMessageStatus.Processing);
        AddCommandParameter(command, "max_publish_attempts", maxPublishAttempts);
        AddCommandParameter(command, "base_retry_delay_ms", baseRetryDelayMilliseconds);
        AddCommandParameter(command, "max_retry_delay_ms", maxRetryDelayMilliseconds);
        AddCommandParameter(command, "processing_lease_timeout_ms", processingLeaseTimeoutMilliseconds);

        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                claimedMessages.Add(new OutboxMessage(
                    reader.GetGuid(0),
                    reader.GetGuid(1),
                    reader.GetString(2),
                    reader.GetString(3),
                    reader.GetString(4),
                    reader.GetInt32(5),
                    reader.GetFieldValue<DateTimeOffset>(6),
                    reader.GetString(7),
                    reader.IsDBNull(8) ? null : reader.GetString(8),
                    reader.IsDBNull(9) ? null : reader.GetGuid(9),
                    (OutboxMessageStatus)reader.GetInt32(10),
                    reader.GetInt32(11),
                    reader.IsDBNull(12) ? null : reader.GetFieldValue<DateTimeOffset>(12),
                    reader.IsDBNull(13) ? null : reader.GetFieldValue<DateTimeOffset>(13),
                    reader.IsDBNull(14) ? null : reader.GetString(14)));
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return claimedMessages;
    }

    public async Task MarkPublishedAsync(Guid messageId, DateTimeOffset publishedAtUtc, CancellationToken cancellationToken = default)
    {
        var affectedRowCount = await dbContext.ProcessRuntimeOutboxMessages
            .Where(outboxMessage =>
                outboxMessage.Id == messageId &&
                outboxMessage.Status == OutboxMessageStatus.Processing)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(outboxMessage => outboxMessage.Status, OutboxMessageStatus.Published)
                    .SetProperty(outboxMessage => outboxMessage.LastAttemptAtUtc, publishedAtUtc)
                    .SetProperty(outboxMessage => outboxMessage.PublishedAtUtc, publishedAtUtc)
                    .SetProperty(outboxMessage => outboxMessage.FailureReason, (string?)null),
                cancellationToken);

        if (affectedRowCount == 0)
        {
            throw new InvalidOperationException($"Outbox message '{messageId}' is no longer in Processing state when marking published.");
        }
    }

    public async Task MarkFailedAsync(Guid messageId, string failureReason, DateTimeOffset failedAtUtc, CancellationToken cancellationToken = default)
    {
        var normalizedFailureReason = string.IsNullOrWhiteSpace(failureReason)
            ? "Process runtime outbox publish failed."
            : failureReason;

        var affectedRowCount = await dbContext.ProcessRuntimeOutboxMessages
            .Where(outboxMessage =>
                outboxMessage.Id == messageId &&
                outboxMessage.Status == OutboxMessageStatus.Processing)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(outboxMessage => outboxMessage.Status, OutboxMessageStatus.Failed)
                    .SetProperty(outboxMessage => outboxMessage.LastAttemptAtUtc, failedAtUtc)
                    .SetProperty(outboxMessage => outboxMessage.FailureReason, normalizedFailureReason),
                cancellationToken);

        if (affectedRowCount == 0)
        {
            throw new InvalidOperationException($"Outbox message '{messageId}' is no longer in Processing state when marking failed.");
        }
    }

    private static ProcessRuntimeOutboxMessage ToEntity(OutboxMessage message)
    {
        return new ProcessRuntimeOutboxMessage
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

    private static void AddCommandParameter(IDbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}