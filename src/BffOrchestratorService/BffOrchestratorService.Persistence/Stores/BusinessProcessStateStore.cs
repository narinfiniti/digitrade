using BffOrchestratorService.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace BffOrchestratorService.Persistence.Stores;

public sealed class BusinessProcessStateStore(BffOrchestratorDbContext dbContext) : IBusinessProcessStateStore
{
    private static readonly string[] ActiveStatuses = ["started", "in_progress", "waiting", "retrying", "compensating", "interrupted", "paused"];

    public async Task AddAsync(Domain.Models.BusinessProcessStateModel processState, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processState);

        await dbContext.BusinessProcessStates.AddAsync(ToEntity(processState), cancellationToken);
    }

    public async Task UpdateAsync(Domain.Models.BusinessProcessStateModel processState, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processState);

        var entity = await dbContext.BusinessProcessStates
            .SingleAsync(state => state.Id == processState.ProcessId, cancellationToken);

        Apply(entity, processState);
    }

    public async Task<Domain.Models.BusinessProcessStateModel?> FindByProcessIdAsync(Guid processId, CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.BusinessProcessStates
            .AsNoTracking()
            .SingleOrDefaultAsync(state => state.Id == processId, cancellationToken);

        return entity is null ? null : ToModel(entity);
    }

    public async Task<Domain.Models.BusinessProcessStateModel?> FindByProcessNameAndIdempotencyKeyAsync(
        string processName,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.BusinessProcessStates
            .AsNoTracking()
            .SingleOrDefaultAsync(
                state => state.ProcessName == processName && state.IdempotencyKey == idempotencyKey,
                cancellationToken);

        return entity is null ? null : ToModel(entity);
    }

    public async Task<Domain.Models.BusinessProcessStateModel?> FindActiveByProcessKeyAsync(
        string processKey,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default)
    {
        var entity = await dbContext.BusinessProcessStates
            .AsNoTracking()
            .Where(state => state.ProcessKey == processKey)
            .Where(state => ActiveStatuses.Contains(state.Status))
            .Where(state => state.NextVisibleAt <= asOfUtc)
            .OrderBy(state => state.NextVisibleAt)
            .ThenBy(state => state.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return entity is null ? null : ToModel(entity);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Domain.Entities.BusinessProcessState ToEntity(Domain.Models.BusinessProcessStateModel processState)
    {
        return new Domain.Entities.BusinessProcessState
        {
            Id = processState.ProcessId,
            ProcessName = processState.ProcessName,
            ProcessKey = processState.ProcessKey,
            AggregateId = processState.AggregateId,
            FlowType = processState.FlowType,
            Status = processState.Status,
            CurrentStepOrdinal = processState.CurrentStepOrdinal,
            CurrentStepName = processState.CurrentStepName,
            Version = processState.Version,
            IdempotencyKey = processState.IdempotencyKey,
            CorrelationId = processState.CorrelationId,
            CausationId = processState.CausationId,
            IntentPersistedAt = processState.IntentPersistedAt,
            SyncDeadlineAt = processState.SyncDeadlineAt,
            RecoveryPolicy = processState.RecoveryPolicy,
            AwaitingClientDecision = processState.AwaitingClientDecision,
            ClientDecisionDeadlineAt = processState.ClientDecisionDeadlineAt,
            RetryCount = processState.RetryCount,
            MaxRetryCount = processState.MaxRetryCount,
            NextVisibleAt = processState.NextVisibleAt,
            LeaseOwner = processState.LeaseOwner,
            LeaseAcquiredAt = processState.LeaseAcquiredAt,
            LeaseExpiresAt = processState.LeaseExpiresAt,
            HeartbeatAt = processState.HeartbeatAt,
            InterruptedAt = processState.InterruptedAt,
            CompletedAt = processState.CompletedAt,
            ResponseStatusCode = processState.ResponseStatusCode,
            ResponseCommittedAt = processState.ResponseCommittedAt,
            LastErrorCode = processState.LastErrorCode,
            LastErrorMessage = processState.LastErrorMessage,
            ProcessContext = processState.ProcessContext,
            InputPayload = processState.InputPayload,
            ResultPayload = processState.ResultPayload,
            CreatedAt = processState.CreatedAt,
            UpdatedAt = processState.UpdatedAt,
        };
    }

    private static Domain.Models.BusinessProcessStateModel ToModel(Domain.Entities.BusinessProcessState entity)
    {
        return new Domain.Models.BusinessProcessStateModel
        {
            ProcessId = entity.Id,
            ProcessName = entity.ProcessName,
            ProcessKey = entity.ProcessKey,
            AggregateId = entity.AggregateId,
            FlowType = entity.FlowType,
            Status = entity.Status,
            CurrentStepOrdinal = entity.CurrentStepOrdinal,
            CurrentStepName = entity.CurrentStepName,
            Version = entity.Version,
            IdempotencyKey = entity.IdempotencyKey,
            CorrelationId = entity.CorrelationId,
            CausationId = entity.CausationId,
            IntentPersistedAt = entity.IntentPersistedAt,
            SyncDeadlineAt = entity.SyncDeadlineAt,
            RecoveryPolicy = entity.RecoveryPolicy,
            AwaitingClientDecision = entity.AwaitingClientDecision,
            ClientDecisionDeadlineAt = entity.ClientDecisionDeadlineAt,
            RetryCount = entity.RetryCount,
            MaxRetryCount = entity.MaxRetryCount,
            NextVisibleAt = entity.NextVisibleAt,
            LeaseOwner = entity.LeaseOwner,
            LeaseAcquiredAt = entity.LeaseAcquiredAt,
            LeaseExpiresAt = entity.LeaseExpiresAt,
            HeartbeatAt = entity.HeartbeatAt,
            InterruptedAt = entity.InterruptedAt,
            CompletedAt = entity.CompletedAt,
            ResponseStatusCode = entity.ResponseStatusCode,
            ResponseCommittedAt = entity.ResponseCommittedAt,
            LastErrorCode = entity.LastErrorCode,
            LastErrorMessage = entity.LastErrorMessage,
            ProcessContext = entity.ProcessContext,
            InputPayload = entity.InputPayload,
            ResultPayload = entity.ResultPayload,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
        };
    }

    private static void Apply(Domain.Entities.BusinessProcessState entity, Domain.Models.BusinessProcessStateModel processState)
    {
        entity.ProcessName = processState.ProcessName;
        entity.ProcessKey = processState.ProcessKey;
        entity.AggregateId = processState.AggregateId;
        entity.FlowType = processState.FlowType;
        entity.Status = processState.Status;
        entity.CurrentStepOrdinal = processState.CurrentStepOrdinal;
        entity.CurrentStepName = processState.CurrentStepName;
        entity.Version = processState.Version;
        entity.IdempotencyKey = processState.IdempotencyKey;
        entity.CorrelationId = processState.CorrelationId;
        entity.CausationId = processState.CausationId;
        entity.IntentPersistedAt = processState.IntentPersistedAt;
        entity.SyncDeadlineAt = processState.SyncDeadlineAt;
        entity.RecoveryPolicy = processState.RecoveryPolicy;
        entity.AwaitingClientDecision = processState.AwaitingClientDecision;
        entity.ClientDecisionDeadlineAt = processState.ClientDecisionDeadlineAt;
        entity.RetryCount = processState.RetryCount;
        entity.MaxRetryCount = processState.MaxRetryCount;
        entity.NextVisibleAt = processState.NextVisibleAt;
        entity.LeaseOwner = processState.LeaseOwner;
        entity.LeaseAcquiredAt = processState.LeaseAcquiredAt;
        entity.LeaseExpiresAt = processState.LeaseExpiresAt;
        entity.HeartbeatAt = processState.HeartbeatAt;
        entity.InterruptedAt = processState.InterruptedAt;
        entity.CompletedAt = processState.CompletedAt;
        entity.ResponseStatusCode = processState.ResponseStatusCode;
        entity.ResponseCommittedAt = processState.ResponseCommittedAt;
        entity.LastErrorCode = processState.LastErrorCode;
        entity.LastErrorMessage = processState.LastErrorMessage;
        entity.ProcessContext = processState.ProcessContext;
        entity.InputPayload = processState.InputPayload;
        entity.ResultPayload = processState.ResultPayload;
        entity.CreatedAt = processState.CreatedAt;
        entity.UpdatedAt = processState.UpdatedAt;
    }
}
