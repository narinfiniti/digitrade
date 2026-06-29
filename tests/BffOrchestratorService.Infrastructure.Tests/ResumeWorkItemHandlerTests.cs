using System.Text.Json;
using BffOrchestratorService.Domain;
using BffOrchestratorService.Domain.Abstractions;
using BffOrchestratorService.Domain.Models;
using BffOrchestratorService.Infrastructure.Services;
using Xunit;

namespace BffOrchestratorService.Infrastructure.Tests;

public sealed class ResumeWorkItemHandlerTests
{
    [Fact]
    public void CanHandleThrowsArgumentNullExceptionWhenQueueItemIsNull()
    {
        var processId = Guid.NewGuid();
        var stateStore = new InMemoryBusinessProcessStateStore(CreateRecoverableState(processId));
        var checkpointStore = new InMemoryProcessCheckpointStore();
        var handler = new ResumeWorkItemHandler(stateStore, checkpointStore, TimeProvider.System);

        Assert.Throws<ArgumentNullException>(() => handler.CanHandle(null!));
    }

    [Fact]
    public async Task HandleAsyncThrowsArgumentNullExceptionWhenQueueItemIsNull()
    {
        var processId = Guid.NewGuid();
        var stateStore = new InMemoryBusinessProcessStateStore(CreateRecoverableState(processId));
        var checkpointStore = new InMemoryProcessCheckpointStore();
        var handler = new ResumeWorkItemHandler(stateStore, checkpointStore, TimeProvider.System);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            handler.HandleAsync(null!, "lease-owner-1", CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsyncThrowsArgumentExceptionWhenLeaseOwnerIsBlank()
    {
        var processId = Guid.NewGuid();
        var stateStore = new InMemoryBusinessProcessStateStore(CreateRecoverableState(processId));
        var checkpointStore = new InMemoryProcessCheckpointStore();
        var handler = new ResumeWorkItemHandler(stateStore, checkpointStore, TimeProvider.System);

        var queueItem = CreateResumeQueueItem(processId, queueItemId: 4000);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.HandleAsync(queueItem, " ", CancellationToken.None));
    }

    [Fact]
    public void CanHandleReturnsFalseForNonResumeWorkType()
    {
        var processId = Guid.NewGuid();
        var stateStore = new InMemoryBusinessProcessStateStore(CreateRecoverableState(processId));
        var checkpointStore = new InMemoryProcessCheckpointStore();
        var handler = new ResumeWorkItemHandler(stateStore, checkpointStore, TimeProvider.System);

        var queueItem = CreateResumeQueueItem(processId, queueItemId: 4001);
        queueItem.WorkType = "timeout";

        Assert.False(handler.CanHandle(queueItem));
    }

    [Fact]
    public void CanHandleReturnsFalseForUnsupportedRecoveryPolicy()
    {
        var processId = Guid.NewGuid();
        var stateStore = new InMemoryBusinessProcessStateStore(CreateRecoverableState(processId));
        var checkpointStore = new InMemoryProcessCheckpointStore();
        var handler = new ResumeWorkItemHandler(stateStore, checkpointStore, TimeProvider.System);

        var queueItem = CreateResumeQueueItem(processId, queueItemId: 4002);
        queueItem.Payload = JsonSerializer.Serialize(new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["recoveryPolicy"] = "alert_client_and_pause",
            ["reason"] = "resume_on_restart",
        });

        Assert.False(handler.CanHandle(queueItem));
    }

    [Fact]
    public void CanHandleReturnsFalseForMixedCaseWorkType()
    {
        var processId = Guid.NewGuid();
        var stateStore = new InMemoryBusinessProcessStateStore(CreateRecoverableState(processId));
        var checkpointStore = new InMemoryProcessCheckpointStore();
        var handler = new ResumeWorkItemHandler(stateStore, checkpointStore, TimeProvider.System);

        var queueItem = CreateResumeQueueItem(processId, queueItemId: 4003);
        queueItem.WorkType = "ReSuMe";

        Assert.False(handler.CanHandle(queueItem));
    }

    [Fact]
    public void CanHandleReturnsFalseForMixedCaseRecoveryPolicy()
    {
        var processId = Guid.NewGuid();
        var stateStore = new InMemoryBusinessProcessStateStore(CreateRecoverableState(processId));
        var checkpointStore = new InMemoryProcessCheckpointStore();
        var handler = new ResumeWorkItemHandler(stateStore, checkpointStore, TimeProvider.System);

        var queueItem = CreateResumeQueueItem(processId, queueItemId: 4004);
        queueItem.Payload = JsonSerializer.Serialize(new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["recoveryPolicy"] = "Resume_On_Restart",
            ["reason"] = "resume_on_restart",
        });

        Assert.False(handler.CanHandle(queueItem));
    }

    [Fact]
    public void CanHandleReturnsFalseForNonStringRecoveryPolicy()
    {
        var processId = Guid.NewGuid();
        var stateStore = new InMemoryBusinessProcessStateStore(CreateRecoverableState(processId));
        var checkpointStore = new InMemoryProcessCheckpointStore();
        var handler = new ResumeWorkItemHandler(stateStore, checkpointStore, TimeProvider.System);

        var queueItem = CreateResumeQueueItem(processId, queueItemId: 4005);
        queueItem.Payload = "{\"recoveryPolicy\":123,\"reason\":\"resume_on_restart\"}";

        Assert.False(handler.CanHandle(queueItem));
    }

    [Fact]
    public async Task HandleAsyncWhenProcessIsTerminalDoesNotMutateStateOrCheckpoint()
    {
        var processId = Guid.NewGuid();
        var state = CreateRecoverableState(processId);
        state.Status = "completed";
        var originalVersion = state.Version;

        var stateStore = new InMemoryBusinessProcessStateStore(state);
        var checkpointStore = new InMemoryProcessCheckpointStore();
        var handler = new ResumeWorkItemHandler(stateStore, checkpointStore, TimeProvider.System);

        var queueItem = CreateResumeQueueItem(processId, queueItemId: 4101);
        await handler.HandleAsync(queueItem, "lease-owner-1", CancellationToken.None);

        Assert.Equal("completed", stateStore.State.Status);
        Assert.Equal(originalVersion, stateStore.State.Version);
        Assert.Empty(checkpointStore.Items);
    }

    [Fact]
    public async Task HandleAsyncWhenProcessStatusIsNotRecoverableDoesNotMutateStateOrCheckpoint()
    {
        var processId = Guid.NewGuid();
        var state = CreateRecoverableState(processId);
        state.Status = "waiting";
        var originalVersion = state.Version;

        var stateStore = new InMemoryBusinessProcessStateStore(state);
        var checkpointStore = new InMemoryProcessCheckpointStore();
        var handler = new ResumeWorkItemHandler(stateStore, checkpointStore, TimeProvider.System);

        var queueItem = CreateResumeQueueItem(processId, queueItemId: 4102);
        await handler.HandleAsync(queueItem, "lease-owner-1", CancellationToken.None);

        Assert.Equal("waiting", stateStore.State.Status);
        Assert.Equal(originalVersion, stateStore.State.Version);
        Assert.Empty(checkpointStore.Items);
    }

    [Fact]
    public async Task HandleAsyncWhenRecoveryPolicyIsUnsupportedThrowsInvalidOperationException()
    {
        var processId = Guid.NewGuid();
        var state = CreateRecoverableState(processId);
        state.RecoveryPolicy = "alert_client_and_pause";

        var stateStore = new InMemoryBusinessProcessStateStore(state);
        var checkpointStore = new InMemoryProcessCheckpointStore();
        var handler = new ResumeWorkItemHandler(stateStore, checkpointStore, TimeProvider.System);

        var queueItem = CreateResumeQueueItem(processId, queueItemId: 4103);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(queueItem, "lease-owner-1", CancellationToken.None));

        Assert.Empty(checkpointStore.Items);
    }

    [Fact]
    public async Task HandleAsyncWhenResumedCheckpointAlreadyExistsDoesNotMutateStateOrCheckpoint()
    {
        var processId = Guid.NewGuid();
        var state = CreateRecoverableState(processId);
        var originalVersion = state.Version;
        var originalStepOrdinal = state.CurrentStepOrdinal;

        var stateStore = new InMemoryBusinessProcessStateStore(state);
        var checkpointStore = new InMemoryProcessCheckpointStore();
        var handler = new ResumeWorkItemHandler(stateStore, checkpointStore, TimeProvider.System);

        var queueItem = CreateResumeQueueItem(processId, queueItemId: 4104);
        checkpointStore.Items.Add(new ProcessCheckpointModel
        {
            ProcessId = processId,
            StepOrdinal = originalStepOrdinal,
            StepName = "resume",
            CheckpointKind = "resumed",
            ObservedOutcome = "pending",
            DispatchId = Guid.NewGuid(),
            IdempotencyKey = $"{processId:N}:resume:handled:4104",
            CheckpointData = "{}",
            OccurredAt = TimeProvider.System.GetUtcNow(),
            CreatedAt = TimeProvider.System.GetUtcNow(),
        });

        await handler.HandleAsync(queueItem, "lease-owner-1", CancellationToken.None);

        Assert.Equal("interrupted", stateStore.State.Status);
        Assert.Equal(originalVersion, stateStore.State.Version);
        Assert.Equal(originalStepOrdinal, stateStore.State.CurrentStepOrdinal);
        Assert.Single(checkpointStore.Items);
    }

    [Fact]
    public async Task HandleAsyncWhenProcessStateIsMissingThrowsInvalidOperationException()
    {
        var existingProcessId = Guid.NewGuid();
        var missingProcessId = Guid.NewGuid();
        var stateStore = new InMemoryBusinessProcessStateStore(CreateRecoverableState(existingProcessId));
        var checkpointStore = new InMemoryProcessCheckpointStore();
        var handler = new ResumeWorkItemHandler(stateStore, checkpointStore, TimeProvider.System);

        var queueItem = CreateResumeQueueItem(missingProcessId, queueItemId: 4105);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(queueItem, "lease-owner-1", CancellationToken.None));

        Assert.Empty(checkpointStore.Items);
    }

    [Fact]
    public async Task HandleAsyncUsesResumeQueueItemSpecificIdempotencyAcrossRecoveryCycles()
    {
        var processId = Guid.NewGuid();
        var stateStore = new InMemoryBusinessProcessStateStore(CreateRecoverableState(processId));
        var checkpointStore = new InMemoryProcessCheckpointStore();
        var handler = new ResumeWorkItemHandler(stateStore, checkpointStore, TimeProvider.System);

        var firstResumeItem = CreateResumeQueueItem(processId, queueItemId: 3001);
        await handler.HandleAsync(firstResumeItem, "lease-owner-1", CancellationToken.None);

        stateStore.State.Status = "retrying";
        stateStore.State.CurrentStepName = "resume_scheduled";
        stateStore.State.InterruptedAt = TimeProvider.System.GetUtcNow();

        var secondResumeItem = CreateResumeQueueItem(processId, queueItemId: 3002);
        await handler.HandleAsync(secondResumeItem, "lease-owner-1", CancellationToken.None);

        var resumedCheckpoints = checkpointStore.Items.Where(checkpoint => checkpoint.CheckpointKind == "resumed").ToList();

        Assert.Equal(2, resumedCheckpoints.Count);
        Assert.Contains(resumedCheckpoints, checkpoint => checkpoint.IdempotencyKey == $"{processId:N}:resume:handled:3001");
        Assert.Contains(resumedCheckpoints, checkpoint => checkpoint.IdempotencyKey == $"{processId:N}:resume:handled:3002");
    }

    [Fact]
    public async Task HandleAsyncWhenResumedTransitionsToWaitingAndClearsInterruptAndErrors()
    {
        var processId = Guid.NewGuid();
        var state = CreateRecoverableState(processId);
        state.Status = "retrying";
        state.AwaitingClientDecision = true;
        state.ClientDecisionDeadlineAt = TimeProvider.System.GetUtcNow().AddMinutes(15);
        state.LastErrorCode = "resume_retry_exhausted";
        state.LastErrorMessage = "retry limit reached";
        var originalVersion = state.Version;
        var originalStepOrdinal = state.CurrentStepOrdinal;

        var stateStore = new InMemoryBusinessProcessStateStore(state);
        var checkpointStore = new InMemoryProcessCheckpointStore();
        var handler = new ResumeWorkItemHandler(stateStore, checkpointStore, TimeProvider.System);

        var queueItem = CreateResumeQueueItem(processId, queueItemId: 4106);
        await handler.HandleAsync(queueItem, "lease-owner-1", CancellationToken.None);

        Assert.Equal("waiting", stateStore.State.Status);
        Assert.Equal("resumed", stateStore.State.CurrentStepName);
        Assert.Equal(originalStepOrdinal + 1, stateStore.State.CurrentStepOrdinal);
        Assert.Equal(originalVersion + 1, stateStore.State.Version);
        Assert.False(stateStore.State.AwaitingClientDecision);
        Assert.Null(stateStore.State.ClientDecisionDeadlineAt);
        Assert.Null(stateStore.State.InterruptedAt);
        Assert.Null(stateStore.State.LastErrorCode);
        Assert.Null(stateStore.State.LastErrorMessage);

        Assert.Single(checkpointStore.Items);
        Assert.Equal("resumed", checkpointStore.Items[0].CheckpointKind);
        Assert.Equal($"{processId:N}:resume:handled:4106", checkpointStore.Items[0].IdempotencyKey);
    }

    [Fact]
    public async Task HandleAsyncWhenReasonMissingUsesResumeOnRestartFallbackInCheckpointData()
    {
        var processId = Guid.NewGuid();
        var stateStore = new InMemoryBusinessProcessStateStore(CreateRecoverableState(processId));
        var checkpointStore = new InMemoryProcessCheckpointStore();
        var handler = new ResumeWorkItemHandler(stateStore, checkpointStore, TimeProvider.System);

        var queueItem = CreateResumeQueueItem(processId, queueItemId: 4107);
        queueItem.Payload = JsonSerializer.Serialize(new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["recoveryPolicy"] = "resume_on_restart",
        });

        await handler.HandleAsync(queueItem, "lease-owner-1", CancellationToken.None);

        Assert.Single(checkpointStore.Items);
        using var checkpointData = JsonDocument.Parse(checkpointStore.Items[0].CheckpointData);
        Assert.Equal("resume_on_restart", checkpointData.RootElement.GetProperty("reason").GetString());
    }

    [Fact]
    public async Task HandleAsyncWhenReasonProvidedPreservesReasonInCheckpointData()
    {
        var processId = Guid.NewGuid();
        var stateStore = new InMemoryBusinessProcessStateStore(CreateRecoverableState(processId));
        var checkpointStore = new InMemoryProcessCheckpointStore();
        var handler = new ResumeWorkItemHandler(stateStore, checkpointStore, TimeProvider.System);

        var queueItem = CreateResumeQueueItem(processId, queueItemId: 4109);
        queueItem.Payload = JsonSerializer.Serialize(new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["recoveryPolicy"] = "resume_on_restart",
            ["reason"] = "explicit_resume_reason",
        });

        await handler.HandleAsync(queueItem, "lease-owner-1", CancellationToken.None);

        Assert.Single(checkpointStore.Items);
        using var checkpointData = JsonDocument.Parse(checkpointStore.Items[0].CheckpointData);
        Assert.Equal("explicit_resume_reason", checkpointData.RootElement.GetProperty("reason").GetString());
    }

    [Fact]
    public async Task HandleAsyncWhenResumedCheckpointCapturesPreviousStateContext()
    {
        var processId = Guid.NewGuid();
        var state = CreateRecoverableState(processId);
        state.Status = "retrying";
        state.AwaitingClientDecision = true;
        state.ClientDecisionDeadlineAt = TimeProvider.System.GetUtcNow().AddMinutes(7);
        state.LastErrorCode = "resume_retry_exhausted";
        state.LastErrorMessage = "retry limit reached";

        var stateStore = new InMemoryBusinessProcessStateStore(state);
        var checkpointStore = new InMemoryProcessCheckpointStore();
        var handler = new ResumeWorkItemHandler(stateStore, checkpointStore, TimeProvider.System);

        var queueItem = CreateResumeQueueItem(processId, queueItemId: 4108);
        await handler.HandleAsync(queueItem, "lease-owner-ctx", CancellationToken.None);

        Assert.Single(checkpointStore.Items);
        var checkpoint = checkpointStore.Items[0];
        Assert.Equal("resumed", checkpoint.CheckpointKind);

        using var checkpointData = JsonDocument.Parse(checkpoint.CheckpointData);
        Assert.Equal("retrying", checkpointData.RootElement.GetProperty("previousStatus").GetString());
        Assert.True(checkpointData.RootElement.GetProperty("previousAwaitingClientDecision").GetBoolean());
        Assert.Equal("resume_retry_exhausted", checkpointData.RootElement.GetProperty("previousLastErrorCode").GetString());
        Assert.Equal("retry limit reached", checkpointData.RootElement.GetProperty("previousLastErrorMessage").GetString());
        Assert.Equal(4108, checkpointData.RootElement.GetProperty("triggerQueueItemId").GetInt64());
        Assert.Equal("lease-owner-ctx", checkpointData.RootElement.GetProperty("leaseOwner").GetString());
    }

    [Fact]
    public async Task HandleAsyncPersistsTriggerAttemptMetadataInResumedCheckpoint()
    {
        var processId = Guid.NewGuid();
        var stateStore = new InMemoryBusinessProcessStateStore(CreateRecoverableState(processId));
        var checkpointStore = new InMemoryProcessCheckpointStore();
        var handler = new ResumeWorkItemHandler(stateStore, checkpointStore, TimeProvider.System);

        var queueItem = CreateResumeQueueItem(processId, queueItemId: 4110);
        queueItem.AttemptCount = 6;
        queueItem.MaxAttemptCount = 11;

        await handler.HandleAsync(queueItem, "lease-owner-1", CancellationToken.None);

        Assert.Single(checkpointStore.Items);
        using var checkpointData = JsonDocument.Parse(checkpointStore.Items[0].CheckpointData);
        Assert.Equal(6, checkpointData.RootElement.GetProperty("triggerAttemptCount").GetInt32());
        Assert.Equal(11, checkpointData.RootElement.GetProperty("triggerMaxAttemptCount").GetInt32());
        Assert.Equal(stateStore.State.CorrelationId, checkpointData.RootElement.GetProperty("correlationId").GetString());
        Assert.Equal("resume_on_restart", checkpointData.RootElement.GetProperty("recoveryPolicy").GetString());
    }

    [Fact]
    public void CanHandleReturnsFalseForMalformedPayload()
    {
        var processId = Guid.NewGuid();
        var stateStore = new InMemoryBusinessProcessStateStore(CreateRecoverableState(processId));
        var checkpointStore = new InMemoryProcessCheckpointStore();
        var handler = new ResumeWorkItemHandler(stateStore, checkpointStore, TimeProvider.System);

        var malformedPayloadItem = new ProcessQueueItemModel
        {
            Id = 3999,
            ProcessId = processId,
            ProcessKey = "trade:account-1",
            FlowType = "synchronous",
            WorkType = "resume",
            Status = "ready",
            Priority = 100,
            VisibleAt = TimeProvider.System.GetUtcNow(),
            SequenceNo = 3999,
            AttemptCount = 1,
            MaxAttemptCount = 5,
            DedupeKey = "resume:malformed",
            Payload = "{ malformed json",
            CreatedAt = TimeProvider.System.GetUtcNow(),
            UpdatedAt = TimeProvider.System.GetUtcNow(),
        };

        Assert.False(handler.CanHandle(malformedPayloadItem));
    }

    private static BusinessProcessStateModel CreateRecoverableState(Guid processId)
    {
        var now = TimeProvider.System.GetUtcNow();

        return new BusinessProcessStateModel
        {
            ProcessId = processId,
            ProcessName = "trade_sync_flow",
            ProcessKey = "trade:account-1",
            FlowType = "synchronous",
            Status = "interrupted",
            CurrentStepOrdinal = 10,
            CurrentStepName = "resume_scheduled",
            Version = 3,
            IdempotencyKey = $"{processId:N}:idem",
            CorrelationId = processId.ToString("N"),
            IntentPersistedAt = now,
            RecoveryPolicy = "resume_on_restart",
            RetryCount = 2,
            MaxRetryCount = 3,
            NextVisibleAt = now,
            InterruptedAt = now,
            ProcessContext = "{}",
            InputPayload = "{}",
            ResultPayload = "{}",
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    private static ProcessQueueItemModel CreateResumeQueueItem(Guid processId, long queueItemId)
    {
        var now = TimeProvider.System.GetUtcNow();

        return new ProcessQueueItemModel
        {
            Id = queueItemId,
            ProcessId = processId,
            ProcessKey = "trade:account-1",
            FlowType = "synchronous",
            WorkType = "resume",
            Status = "ready",
            Priority = 100,
            VisibleAt = now,
            SequenceNo = queueItemId,
            AttemptCount = 1,
            MaxAttemptCount = 5,
            DedupeKey = $"resume:{queueItemId}",
            Payload = JsonSerializer.Serialize(new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["recoveryPolicy"] = "resume_on_restart",
                ["reason"] = "resume_on_restart",
            }),
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    private sealed class InMemoryBusinessProcessStateStore(BusinessProcessStateModel state) : IBusinessProcessStateStore
    {
        public BusinessProcessStateModel State { get; set; } = state;

        public Task AddAsync(BusinessProcessStateModel processState, CancellationToken cancellationToken = default)
        {
            State = processState;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(BusinessProcessStateModel processState, CancellationToken cancellationToken = default)
        {
            State = processState;
            return Task.CompletedTask;
        }

        public Task<BusinessProcessStateModel?> FindByProcessIdAsync(Guid processId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(State.ProcessId == processId ? State : null);
        }

        public Task<BusinessProcessStateModel?> FindByProcessNameAndIdempotencyKeyAsync(string processName, string idempotencyKey, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<BusinessProcessStateModel?>(null);
        }

        public Task<BusinessProcessStateModel?> FindActiveByProcessKeyAsync(string processKey, DateTimeOffset asOfUtc, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<BusinessProcessStateModel?>(null);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(1);
        }
    }

    private sealed class InMemoryProcessCheckpointStore : IProcessCheckpointStore
    {
        public List<ProcessCheckpointModel> Items { get; } = [];

        public Task AddAsync(ProcessCheckpointModel checkpoint, CancellationToken cancellationToken = default)
        {
            Items.Add(checkpoint);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(Guid processId, string checkpointKind, string idempotencyKey, CancellationToken cancellationToken = default)
        {
            var exists = Items.Any(checkpoint =>
                checkpoint.ProcessId == processId
                && string.Equals(checkpoint.CheckpointKind, checkpointKind, StringComparison.Ordinal)
                && string.Equals(checkpoint.IdempotencyKey, idempotencyKey, StringComparison.Ordinal));

            return Task.FromResult(exists);
        }

        public Task<IReadOnlyCollection<ProcessCheckpointModel>> ListByProcessIdAsync(Guid processId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<ProcessCheckpointModel>>(Items.Where(item => item.ProcessId == processId).ToList());
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(1);
        }
    }
}
