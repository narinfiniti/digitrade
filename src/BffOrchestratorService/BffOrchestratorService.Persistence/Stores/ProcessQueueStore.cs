using System.Data;
using System.Globalization;
using System.Text.Json;
using AutoMapper;
using BffOrchestratorService.Domain;
using BffOrchestratorService.Domain.Abstractions;
using BffOrchestratorService.Domain.Entities;
using DigiTrade.Messaging.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BffOrchestratorService.Persistence.Stores;

public sealed class ProcessQueueStore(BffOrchestratorDbContext dbContext, IMapper mapper) : IProcessQueueStore
{
    public async Task AddAsync(Domain.Models.ProcessQueueItemModel queueItem, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queueItem);

        await dbContext.ProcessQueueItems.AddAsync(mapper.Map<Domain.Entities.ProcessQueueItem>(queueItem), cancellationToken);
    }

    public async Task UpdateAsync(Domain.Models.ProcessQueueItemModel queueItem, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queueItem);

        var entity = await dbContext.ProcessQueueItems
            .SingleAsync(item => item.Id == queueItem.Id, cancellationToken);

        mapper.Map(queueItem, entity);
    }

    public async Task<Domain.Models.ProcessQueueItemModel?> FindByDedupeKeyAsync(string dedupeKey, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.ProcessQueueItems
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.DedupeKey == dedupeKey, cancellationToken);

        return entity is null ? null : mapper.Map<Domain.Models.ProcessQueueItemModel>(entity);
    }

    public async Task<IReadOnlyCollection<Domain.Models.ProcessQueueItemModel>> LeaseReadyAsync(
        int batchSize,
        DateTimeOffset asOfUtc,
        string leaseOwner,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(leaseOwner);

        if (leaseDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(leaseDuration), "Lease duration must be positive.");
        }

        var normalizedBatchSize = Math.Max(batchSize, 1);
        var leaseAcquiredAt = asOfUtc;
        var leaseExpiresAt = asOfUtc.Add(leaseDuration);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        var leasedQueueItemIds = await LeaseReadyQueueItemsAsync(
            normalizedBatchSize,
            asOfUtc,
            leaseOwner,
            leaseAcquiredAt,
            leaseExpiresAt,
            transaction,
            cancellationToken);

        if (leasedQueueItemIds.Count == 0)
        {
            await transaction.CommitAsync(cancellationToken);
            return Array.Empty<Domain.Models.ProcessQueueItemModel>();
        }

        var entities = await dbContext.ProcessQueueItems
            .AsNoTracking()
            .Where(item => leasedQueueItemIds.Contains(item.Id))
            .OrderBy(item => item.Priority)
            .ThenBy(item => item.VisibleAt)
            .ThenBy(item => item.SequenceNo)
            .ToListAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return entities.Select(entity => mapper.Map<Domain.Models.ProcessQueueItemModel>(entity)).ToArray();
    }

    public async Task<bool> RenewLeaseAsync(
        long queueItemId,
        DateTimeOffset asOfUtc,
        string leaseOwner,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(leaseOwner);

        if (leaseDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(leaseDuration), "Lease duration must be positive.");
        }

        var leaseExpiresAt = asOfUtc.Add(leaseDuration);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var connection = dbContext.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = """
            with renewed_queue as (
                update process_queue pq
                set lease_expires_at = @lease_expires_at,
                    updated_at = @as_of_utc
                where pq.Id = @queue_item_id
                  and pq.status = 'leased'
                  and pq.lease_owner = @lease_owner
                  and pq.lease_expires_at is not null
                  and pq.lease_expires_at >= @as_of_utc
                returning pq.process_id
            ),
            renewed_process_state as (
                update business_process_state bps
                set lease_expires_at = @lease_expires_at,
                    heartbeat_at = @as_of_utc,
                    updated_at = @as_of_utc
                from renewed_queue renewed
                where bps.process_id = renewed.process_id
                  and bps.lease_owner = @lease_owner
                returning bps.process_id
            )
            select case
                when exists (select 1 from renewed_queue)
                 and exists (select 1 from renewed_process_state)
                then 1
                else 0
            end;
            """;

        AddCommandParameter(command, "queue_item_id", queueItemId);
        AddCommandParameter(command, "as_of_utc", asOfUtc);
        AddCommandParameter(command, "lease_owner", leaseOwner);
        AddCommandParameter(command, "lease_expires_at", leaseExpiresAt);

        var renewed = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture) == 1;

        if (renewed)
        {
            await transaction.CommitAsync(cancellationToken);
            return true;
        }

        await transaction.RollbackAsync(cancellationToken);
        return false;
    }

    public async Task<bool> CompleteAsync(
        long queueItemId,
        DateTimeOffset asOfUtc,
        string leaseOwner,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(leaseOwner);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var connection = dbContext.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = """
            with completed_queue as (
                update process_queue pq
                set status = 'completed',
                    lease_owner = null,
                    lease_acquired_at = null,
                    lease_expires_at = null,
                    updated_at = @as_of_utc
                where pq.Id = @queue_item_id
                  and pq.status = 'leased'
                  and pq.lease_owner = @lease_owner
                  and pq.lease_expires_at is not null
                  and pq.lease_expires_at >= @as_of_utc
                returning pq.process_id
            ),
            released_process_state as (
                update business_process_state bps
                set lease_owner = null,
                    lease_acquired_at = null,
                    lease_expires_at = null,
                    heartbeat_at = null,
                    updated_at = @as_of_utc
                from completed_queue completed
                where bps.process_id = completed.process_id
                  and bps.lease_owner = @lease_owner
                returning bps.process_id
            )
            select case
                when exists (select 1 from completed_queue)
                 and exists (select 1 from released_process_state)
                then 1
                else 0
            end;
            """;

        AddCommandParameter(command, "queue_item_id", queueItemId);
        AddCommandParameter(command, "as_of_utc", asOfUtc);
        AddCommandParameter(command, "lease_owner", leaseOwner);

        var completed = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture) == 1;

        if (completed)
        {
            await transaction.CommitAsync(cancellationToken);
            return true;
        }

        await transaction.RollbackAsync(cancellationToken);
        return false;
    }

    public async Task<bool> DeadLetterAsync(
        long queueItemId,
        DateTimeOffset asOfUtc,
        string leaseOwner,
        string errorCode,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(leaseOwner);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var connection = dbContext.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = """
            with dead_lettered_queue as (
                update process_queue pq
                set status = 'dead_letter',
                    lease_owner = null,
                    lease_acquired_at = null,
                    lease_expires_at = null,
                    last_error_code = @error_code,
                    last_error_message = @error_message,
                    updated_at = @as_of_utc
                where pq.Id = @queue_item_id
                  and pq.status = 'leased'
                  and pq.lease_owner = @lease_owner
                  and pq.lease_expires_at is not null
                  and pq.lease_expires_at >= @as_of_utc
                returning pq.process_id
            ),
            released_process_state as (
                update business_process_state bps
                set lease_owner = null,
                    lease_acquired_at = null,
                    lease_expires_at = null,
                    heartbeat_at = null,
                    status = 'failed',
                    current_step_ordinal = bps.current_step_ordinal + 1,
                    current_step_name = 'supervision_dead_letter',
                    version = bps.version + 1,
                    last_error_code = @error_code,
                    last_error_message = @error_message,
                    updated_at = @as_of_utc
                from dead_lettered_queue dead_lettered
                where bps.process_id = dead_lettered.process_id
                  and bps.lease_owner = @lease_owner
                returning bps.process_id, bps.current_step_ordinal
            ),
            inserted_checkpoint as (
                insert into process_checkpoint (
                    process_id,
                    step_ordinal,
                    step_name,
                    checkpoint_kind,
                    observed_outcome,
                    dispatch_id,
                    idempotency_key,
                    checkpoint_data,
                    occurred_at,
                    created_at)
                select
                    released.process_id,
                    released.current_step_ordinal,
                    'supervision_dead_letter',
                    'step_failed',
                    'failed',
                    @dispatch_id,
                    concat(@queue_item_id, ':dead_letter'),
                    jsonb_build_object(
                        'queueItemId', @queue_item_id,
                        'errorCode', @error_code,
                        'errorMessage', @error_message),
                    @as_of_utc,
                    @as_of_utc
                from released_process_state released
                returning Id
            )
                        select released.process_id
                        from released_process_state released
                        where exists (select 1 from dead_lettered_queue)
                            and exists (select 1 from inserted_checkpoint)
                        limit 1;
            """;

        AddCommandParameter(command, "queue_item_id", queueItemId);
        AddCommandParameter(command, "as_of_utc", asOfUtc);
        AddCommandParameter(command, "lease_owner", leaseOwner);
        AddCommandParameter(command, "error_code", errorCode);
        AddCommandParameter(command, "error_message", errorMessage);
        AddCommandParameter(command, "dispatch_id", Guid.NewGuid());

        var processIdResult = await command.ExecuteScalarAsync(cancellationToken);

        if (processIdResult is Guid processId)
        {
            var processState = await dbContext.BusinessProcessStates
                .AsNoTracking()
                .SingleAsync(state => state.Id == processId, cancellationToken);

            var outboxMessage = new ProcessRuntimeOutboxMessage
            {
                Id = Guid.NewGuid(),
                EventId = Guid.NewGuid(),
                EventName = "BusinessProcessInterrupted",
                AggregateId = processState.Id.ToString("D"),
                PartitionKey = processState.ProcessKey,
                EventVersion = 1,
                OccurredAtUtc = asOfUtc,
                Payload = JsonSerializer.Serialize(new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["eventType"] = "BusinessProcessInterrupted",
                    ["processId"] = processState.Id,
                    ["processName"] = processState.ProcessName,
                    ["status"] = processState.Status,
                    ["recoveryPolicy"] = processState.RecoveryPolicy,
                    ["correlationId"] = processState.CorrelationId,
                    ["reason"] = "supervision_dead_letter",
                    ["errorCode"] = errorCode,
                    ["errorMessage"] = errorMessage,
                }),
                HeadersJson = JsonSerializer.Serialize(new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["correlation-id"] = processState.CorrelationId,
                    ["process-id"] = processState.Id.ToString("D"),
                }),
                Status = OutboxMessageStatus.Pending,
                AttemptCount = 0,
            };

            dbContext.ProcessRuntimeOutboxMessages.Add(outboxMessage);
            await dbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return true;
        }

        await transaction.RollbackAsync(cancellationToken);
        return false;
    }

    public async Task<bool> RequeueAsync(
        long queueItemId,
        DateTimeOffset asOfUtc,
        DateTimeOffset visibleAt,
        string leaseOwner,
        string errorCode,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(leaseOwner);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        var connection = dbContext.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = """
            with requeued_queue as (
                update process_queue pq
                set status = 'ready',
                    visible_at = @visible_at,
                    lease_owner = null,
                    lease_acquired_at = null,
                    lease_expires_at = null,
                    last_error_code = @error_code,
                    last_error_message = @error_message,
                    updated_at = @as_of_utc
                where pq.Id = @queue_item_id
                  and pq.status = 'leased'
                  and pq.lease_owner = @lease_owner
                  and pq.lease_expires_at is not null
                  and pq.lease_expires_at >= @as_of_utc
                returning pq.process_id
            ),
            released_process_state as (
                update business_process_state bps
                set lease_owner = null,
                    lease_acquired_at = null,
                    lease_expires_at = null,
                    heartbeat_at = null,
                    status = 'retrying',
                    current_step_ordinal = bps.current_step_ordinal + 1,
                    current_step_name = 'supervision_retry_scheduled',
                    version = bps.version + 1,
                    retry_count = least(bps.retry_count + 1, bps.max_retry_count),
                    next_visible_at = @visible_at,
                    last_error_code = @error_code,
                    last_error_message = @error_message,
                    updated_at = @as_of_utc
                from requeued_queue requeued
                where bps.process_id = requeued.process_id
                  and bps.lease_owner = @lease_owner
                returning bps.process_id, bps.current_step_ordinal
            ),
            inserted_checkpoint as (
                insert into process_checkpoint (
                    process_id,
                    step_ordinal,
                    step_name,
                    checkpoint_kind,
                    observed_outcome,
                    dispatch_id,
                    idempotency_key,
                    checkpoint_data,
                    occurred_at,
                    created_at)
                select
                    released.process_id,
                    released.current_step_ordinal,
                    'supervision_retry',
                    'retry_scheduled',
                    'pending',
                    @dispatch_id,
                    concat(@queue_item_id, ':retry:', @visible_at),
                    jsonb_build_object(
                        'queueItemId', @queue_item_id,
                        'visibleAt', @visible_at,
                        'errorCode', @error_code,
                        'errorMessage', @error_message),
                    @as_of_utc,
                    @as_of_utc
                from released_process_state released
                returning Id
            )
            select case
                when exists (select 1 from requeued_queue)
                 and exists (select 1 from released_process_state)
                 and exists (select 1 from inserted_checkpoint)
                then 1
                else 0
            end;
            """;

        AddCommandParameter(command, "queue_item_id", queueItemId);
        AddCommandParameter(command, "as_of_utc", asOfUtc);
        AddCommandParameter(command, "visible_at", visibleAt);
        AddCommandParameter(command, "lease_owner", leaseOwner);
        AddCommandParameter(command, "error_code", errorCode);
        AddCommandParameter(command, "error_message", errorMessage);
        AddCommandParameter(command, "dispatch_id", Guid.NewGuid());

        var requeued = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture) == 1;

        if (requeued)
        {
            await transaction.CommitAsync(cancellationToken);
            return true;
        }

        await transaction.RollbackAsync(cancellationToken);
        return false;
    }

    public async Task<IReadOnlyCollection<Domain.Models.ProcessQueueItemModel>> ListVisibleAsync(
        int batchSize,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default)
    {
        var normalizedBatchSize = Math.Max(batchSize, 1);

        var entities = await dbContext.ProcessQueueItems
            .AsNoTracking()
            .Where(item => item.VisibleAt <= asOfUtc)
            .OrderBy(item => item.Priority)
            .ThenBy(item => item.VisibleAt)
            .ThenBy(item => item.SequenceNo)
            .Take(normalizedBatchSize)
            .ToListAsync(cancellationToken);

        return entities.Select(entity => mapper.Map<Domain.Models.ProcessQueueItemModel>(entity)).ToArray();
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlyCollection<long>> LeaseReadyQueueItemsAsync(
        int batchSize,
        DateTimeOffset asOfUtc,
        string leaseOwner,
        DateTimeOffset leaseAcquiredAt,
        DateTimeOffset leaseExpiresAt,
        IDbContextTransaction transaction,
        CancellationToken cancellationToken)
    {
        var leasedQueueItemIds = new List<long>(batchSize);
        var connection = dbContext.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = """
            with ranked_queue as (
                select
                    pq."Id",
                    pq.process_id,
                    pq.priority,
                    pq.visible_at,
                    pq.sequence_no,
                    row_number() over (
                        partition by pq.process_key
                        order by pq.priority, pq.visible_at, pq.sequence_no) as lane_rank
                from process_queue pq
                where pq.status = 'ready'
                  and pq.visible_at <= @as_of_utc
            ),
            eligible_queue as (
                select pq."Id", pq.process_id
                from process_queue pq
                inner join ranked_queue ranked on ranked."Id" = pq."Id"
                where ranked.lane_rank = 1
                order by pq.priority, pq.visible_at, pq.sequence_no
                for update of pq skip locked
                limit @batch_size
            ),
            leased_queue as (
                update process_queue pq
                set status = 'leased',
                    lease_owner = @lease_owner,
                    lease_acquired_at = @lease_acquired_at,
                    lease_expires_at = @lease_expires_at,
                    attempt_count = pq.attempt_count + 1,
                    updated_at = @lease_acquired_at
                from eligible_queue eligible
                where pq."Id" = eligible."Id"
                returning pq."Id", pq.process_id
            ),
            updated_process_state as (
                update business_process_state bps
                set status = 'in_progress',
                    lease_owner = @lease_owner,
                    lease_acquired_at = @lease_acquired_at,
                    lease_expires_at = @lease_expires_at,
                    heartbeat_at = @lease_acquired_at,
                    updated_at = @lease_acquired_at
                from leased_queue leased
                where bps."Id" = leased.process_id
                returning bps."Id" as process_id
            )
            select leased."Id"
                from leased_queue leased
                inner join updated_process_state updated
                    on updated.process_id = leased.process_id
            order by leased."Id";
            """;

        AddCommandParameter(command, "as_of_utc", asOfUtc);
        AddCommandParameter(command, "batch_size", batchSize);
        AddCommandParameter(command, "lease_owner", leaseOwner);
        AddCommandParameter(command, "lease_acquired_at", leaseAcquiredAt);
        AddCommandParameter(command, "lease_expires_at", leaseExpiresAt);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            leasedQueueItemIds.Add(reader.GetInt64(0));
        }

        return leasedQueueItemIds;
    }

    private static void AddCommandParameter(IDbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

}
