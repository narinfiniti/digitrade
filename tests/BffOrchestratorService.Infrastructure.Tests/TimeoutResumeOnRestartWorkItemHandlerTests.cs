using System.Text.Json;
using BffOrchestratorService.Domain;
using BffOrchestratorService.Domain.Abstractions;
using BffOrchestratorService.Domain.Models;
using BffOrchestratorService.Infrastructure.Services;
using Xunit;

namespace BffOrchestratorService.Infrastructure.Tests;

public sealed class TimeoutResumeOnRestartWorkItemHandlerTests
{
    [Fact]
    public void CanHandleThrowsArgumentNullExceptionWhenQueueItemIsNull()
    {
        var processId = Guid.NewGuid();
        var stateStore = new InMemoryBusinessProcessStateStore(CreateInterruptedState(processId, retryCount: 0, maxRetryCount: 2));
        var checkpointStore = new InMemoryProcessCheckpointStore();
        var queueStore = new InMemoryProcessQueueStore();
        var handler = new TimeoutResumeOnRestartWorkItemHandler(stateStore, checkpointStore, queueStore, TimeProvider.System);

        Assert.Throws<ArgumentNullException>(() => handler.CanHandle(null!));
    }

    [Fact]
    public async Task HandleAsyncThrowsArgumentNullExceptionWhenQueueItemIsNull()
    {
        var processId = Guid.NewGuid();
        var stateStore = new InMemoryBusinessProcessStateStore(CreateInterruptedState(processId, retryCount: 0, maxRetryCount: 2));
        var checkpointStore = new InMemoryProcessCheckpointStore();
        var queueStore = new InMemoryProcessQueueStore();
        var handler = new TimeoutResumeOnRestartWorkItemHandler(stateStore, checkpointStore, queueStore, TimeProvider.System);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            handler.HandleAsync(null!, "lease-owner-1", CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsyncThrowsArgumentExceptionWhenLeaseOwnerIsBlank()
    {
        var processId = Guid.NewGuid();
        var stateStore = new InMemoryBusinessProcessStateStore(CreateInterruptedState(processId, retryCount: 0, maxRetryCount: 2));
        var checkpointStore = new InMemoryProcessCheckpointStore();
        var queueStore = new InMemoryProcessQueueStore();
        var handler = new TimeoutResumeOnRestartWorkItemHandler(stateStore, checkpointStore, queueStore, TimeProvider.System);

        var queueItem = CreateTimeoutQueueItem(processId, queueItemId: 4999);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.HandleAsync(queueItem, " ", CancellationToken.None));
    }

    [Fact]
    public void CanHandleReturnsFalseForNonTimeoutWorkType()
    {
        var processId = Guid.NewGuid();
        var stateStore = new InMemoryBusinessProcessStateStore(CreateInterruptedState(processId, retryCount: 0, maxRetryCount: 2));
        var checkpointStore = new InMemoryProcessCheckpointStore();
        var queueStore = new InMemoryProcessQueueStore();
        var handler = new TimeoutResumeOnRestartWorkItemHandler(stateStore, checkpointStore, queueStore, TimeProvider.System);

        var queueItem = CreateTimeoutQueueItem(processId, queueItemId: 500);
        queueItem.WorkType = "resume";

        Assert.False(handler.CanHandle(queueItem));
    }

    [Fact]
    public void CanHandleReturnsFalseForUnsupportedRecoveryPolicy()
    {
        var processId = Guid.NewGuid();
        var stateStore = new InMemoryBusinessProcessStateStore(CreateInterruptedState(processId, retryCount: 0, maxRetryCount: 2));
        var checkpointStore = new InMemoryProcessCheckpointStore();
        var queueStore = new InMemoryProcessQueueStore();
        var handler = new TimeoutResumeOnRestartWorkItemHandler(stateStore, checkpointStore, queueStore, TimeProvider.System);

        var queueItem = CreateTimeoutQueueItem(processId, queueItemId: 5000);
        queueItem.Payload = JsonSerializer.Serialize(new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["recoveryPolicy"] = "alert_client_and_pause",
            ["reason"] = "sync_deadline_exceeded",
        });

        Assert.False(handler.CanHandle(queueItem));
    }

    [Fact]
    public void CanHandleReturnsFalseForMixedCaseWorkType()
    {
        var processId = Guid.NewGuid();
        var stateStore = new InMemoryBusinessProcessStateStore(CreateInterruptedState(processId, retryCount: 0, maxRetryCount: 2));
        var checkpointStore = new InMemoryProcessCheckpointStore();
        var queueStore = new InMemoryProcessQueueStore();
        var handler = new TimeoutResumeOnRestartWorkItemHandler(stateStore, checkpointStore, queueStore, TimeProvider.System);

        var queueItem = CreateTimeoutQueueItem(processId, queueItemId: 5001);
        queueItem.WorkType = "TimeOut";

        Assert.False(handler.CanHandle(queueItem));
    }

    [Fact]
    public void CanHandleReturnsFalseForMixedCaseRecoveryPolicy()
    {
        var processId = Guid.NewGuid();
        var stateStore = new InMemoryBusinessProcessStateStore(CreateInterruptedState(processId, retryCount: 0, maxRetryCount: 2));
        var checkpointStore = new InMemoryProcessCheckpointStore();
        var queueStore = new InMemoryProcessQueueStore();
        var handler = new TimeoutResumeOnRestartWorkItemHandler(stateStore, checkpointStore, queueStore, TimeProvider.System);

        var queueItem = CreateTimeoutQueueItem(processId, queueItemId: 5002);
        queueItem.Payload = JsonSerializer.Serialize(new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["recoveryPolicy"] = "Resume_On_Restart",
            ["reason"] = "sync_deadline_exceeded",
        });

        Assert.False(handler.CanHandle(queueItem));
    }

    [Fact]
    public void CanHandleReturnsFalseForNonStringRecoveryPolicy()
    {
        var processId = Guid.NewGuid();
        var stateStore = new InMemoryBusinessProcessStateStore(CreateInterruptedState(processId, retryCount: 0, maxRetryCount: 2));
        var checkpointStore = new InMemoryProcessCheckpointStore();
        var queueStore = new InMemoryProcessQueueStore();
        var handler = new TimeoutResumeOnRestartWorkItemHandler(stateStore, checkpointStore, queueStore, TimeProvider.System);

        var queueItem = CreateTimeoutQueueItem(processId, queueItemId: 5003);
        queueItem.Payload = "{\"recoveryPolicy\":true,\"reason\":\"sync_deadline_exceeded\"}";

        Assert.False(handler.CanHandle(queueItem));
    }

    [Fact]
    public void CanHandleReturnsFalseForMalformedPayload()
    {
        var processId = Guid.NewGuid();
        var stateStore = new InMemoryBusinessProcessStateStore(CreateInterruptedState(processId, retryCount: 0, maxRetryCount: 2));
        var checkpointStore = new InMemoryProcessCheckpointStore();
        var queueStore = new InMemoryProcessQueueStore();
        var handler = new TimeoutResumeOnRestartWorkItemHandler(stateStore, checkpointStore, queueStore, TimeProvider.System);

        var queueItem = CreateTimeoutQueueItem(processId, queueItemId: 501);
        queueItem.Payload = "{ malformed json";

        Assert.False(handler.CanHandle(queueItem));
    }

    [Fact]
    public async Task HandleAsyncWhenProcessIsTerminalDoesNotMutateStateOrEnqueue()
    {
        var processId = Guid.NewGuid();
        var state = CreateInterruptedState(processId, retryCount: 0, maxRetryCount: 2);
        state.Status = "completed";
        var originalVersion = state.Version;

        var stateStore = new InMemoryBusinessProcessStateStore(state);
        var checkpointStore = new InMemoryProcessCheckpointStore();
        var queueStore = new InMemoryProcessQueueStore();
        var handler = new TimeoutResumeOnRestartWorkItemHandler(stateStore, checkpointStore, queueStore, TimeProvider.System);

        var queueItem = CreateTimeoutQueueItem(processId, queueItemId: 502);
        await handler.HandleAsync(queueItem, "lease-owner-1", CancellationToken.None);

        Assert.Equal("completed", stateStore.State.Status);
        Assert.Equal(originalVersion, stateStore.State.Version);
        Assert.Empty(checkpointStore.Items);
        Assert.Empty(queueStore.Items);
    }

    [Fact]
    public async Task HandleAsyncWhenRecoveryPolicyIsUnsupportedThrowsInvalidOperationException()
    {
        var processId = Guid.NewGuid();
        var state = CreateInterruptedState(processId, retryCount: 0, maxRetryCount: 2);
        state.RecoveryPolicy = "alert_client_and_pause";

        var stateStore = new InMemoryBusinessProcessStateStore(state);
        var checkpointStore = new InMemoryProcessCheckpointStore();
        var queueStore = new InMemoryProcessQueueStore();
        var handler = new TimeoutResumeOnRestartWorkItemHandler(stateStore, checkpointStore, queueStore, TimeProvider.System);

        var queueItem = CreateTimeoutQueueItem(processId, queueItemId: 503);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(queueItem, "lease-owner-1", CancellationToken.None));

        Assert.Empty(checkpointStore.Items);
        Assert.Empty(queueStore.Items);
    }

    [Fact]
    public async Task HandleAsyncWhenRetryScheduledCheckpointAlreadyExistsDoesNotMutateOrEnqueue()
    {
        var processId = Guid.NewGuid();
        var state = CreateInterruptedState(processId, retryCount: 1, maxRetryCount: 3);
        var originalVersion = state.Version;
        var originalRetryCount = state.RetryCount;

        var stateStore = new InMemoryBusinessProcessStateStore(state);
        var checkpointStore = new InMemoryProcessCheckpointStore();
        var queueStore = new InMemoryProcessQueueStore();
        var handler = new TimeoutResumeOnRestartWorkItemHandler(stateStore, checkpointStore, queueStore, TimeProvider.System);

        var queueItem = CreateTimeoutQueueItem(processId, queueItemId: 504);
        checkpointStore.Items.Add(new ProcessCheckpointModel
        {
            ProcessId = processId,
            StepOrdinal = state.CurrentStepOrdinal,
            StepName = "resume",
            CheckpointKind = "retry_scheduled",
            ObservedOutcome = "pending",
            DispatchId = Guid.NewGuid(),
            IdempotencyKey = $"{processId:N}:resume:timeout:504",
            CheckpointData = "{}",
            OccurredAt = TimeProvider.System.GetUtcNow(),
            CreatedAt = TimeProvider.System.GetUtcNow(),
        });

        await handler.HandleAsync(queueItem, "lease-owner-1", CancellationToken.None);

        Assert.Equal("interrupted", stateStore.State.Status);
        Assert.Equal(originalVersion, stateStore.State.Version);
        Assert.Equal(originalRetryCount, stateStore.State.RetryCount);
        Assert.Single(checkpointStore.Items);
        Assert.Empty(queueStore.Items);
    }

    [Fact]
    public async Task HandleAsyncWhenRetryExhaustedCheckpointAlreadyExistsDoesNotMutateOrEnqueue()
    {
        var processId = Guid.NewGuid();
        var state = CreateInterruptedState(processId, retryCount: 3, maxRetryCount: 3);
        var originalVersion = state.Version;
        var originalStatus = state.Status;

        var stateStore = new InMemoryBusinessProcessStateStore(state);
        var checkpointStore = new InMemoryProcessCheckpointStore();
        var queueStore = new InMemoryProcessQueueStore();
        var handler = new TimeoutResumeOnRestartWorkItemHandler(stateStore, checkpointStore, queueStore, TimeProvider.System);

        var queueItem = CreateTimeoutQueueItem(processId, queueItemId: 507);
        checkpointStore.Items.Add(new ProcessCheckpointModel
        {
            ProcessId = processId,
            StepOrdinal = state.CurrentStepOrdinal,
            StepName = "resume",
            CheckpointKind = "paused",
            ObservedOutcome = "paused",
            DispatchId = Guid.NewGuid(),
            IdempotencyKey = $"{processId:N}:resume:retry_exhausted:timeout:507",
            CheckpointData = "{}",
            OccurredAt = TimeProvider.System.GetUtcNow(),
            CreatedAt = TimeProvider.System.GetUtcNow(),
        });

        await handler.HandleAsync(queueItem, "lease-owner-1", CancellationToken.None);

        Assert.Equal(originalStatus, stateStore.State.Status);
        Assert.Equal(originalVersion, stateStore.State.Version);
        Assert.Single(checkpointStore.Items);
        Assert.Empty(queueStore.Items);
    }

    [Fact]
    public async Task HandleAsyncWhenResumeQueueItemAlreadyExistsDoesNotMutateOrWriteCheckpoint()
    {
        var processId = Guid.NewGuid();
        var state = CreateInterruptedState(processId, retryCount: 1, maxRetryCount: 3);
        var originalVersion = state.Version;
        var originalRetryCount = state.RetryCount;

        var stateStore = new InMemoryBusinessProcessStateStore(state);
        var checkpointStore = new InMemoryProcessCheckpointStore();
        var queueStore = new InMemoryProcessQueueStore();
        var handler = new TimeoutResumeOnRestartWorkItemHandler(stateStore, checkpointStore, queueStore, TimeProvider.System);

        var queueItem = CreateTimeoutQueueItem(processId, queueItemId: 505);
        queueStore.Items.Add(new ProcessQueueItemModel
        {
            Id = 9999,
            ProcessId = processId,
            ProcessKey = state.ProcessKey,
            FlowType = state.FlowType,
            WorkType = "resume",
            Status = "ready",
            Priority = queueItem.Priority,
            VisibleAt = TimeProvider.System.GetUtcNow(),
            SequenceNo = 1,
            AttemptCount = 0,
            MaxAttemptCount = queueItem.MaxAttemptCount,
            DedupeKey = $"{processId:N}:resume:timeout:505",
            Payload = "{}",
            CreatedAt = TimeProvider.System.GetUtcNow(),
            UpdatedAt = TimeProvider.System.GetUtcNow(),
        });

        await handler.HandleAsync(queueItem, "lease-owner-1", CancellationToken.None);

        Assert.Equal("interrupted", stateStore.State.Status);
        Assert.Equal(originalVersion, stateStore.State.Version);
        Assert.Equal(originalRetryCount, stateStore.State.RetryCount);
        Assert.Empty(checkpointStore.Items);
        Assert.Single(queueStore.Items);
    }

    [Fact]
    public async Task HandleAsyncWhenProcessStatusIsNotInterruptedDoesNotMutateOrWrite()
    {
        var processId = Guid.NewGuid();
        var state = CreateInterruptedState(processId, retryCount: 1, maxRetryCount: 3);
        state.Status = "waiting";
        var originalVersion = state.Version;
        var originalRetryCount = state.RetryCount;

        var stateStore = new InMemoryBusinessProcessStateStore(state);
        var checkpointStore = new InMemoryProcessCheckpointStore();
        var queueStore = new InMemoryProcessQueueStore();
        var handler = new TimeoutResumeOnRestartWorkItemHandler(stateStore, checkpointStore, queueStore, TimeProvider.System);

        var queueItem = CreateTimeoutQueueItem(processId, queueItemId: 506);
        await handler.HandleAsync(queueItem, "lease-owner-1", CancellationToken.None);

        Assert.Equal("waiting", stateStore.State.Status);
        Assert.Equal(originalVersion, stateStore.State.Version);
        Assert.Equal(originalRetryCount, stateStore.State.RetryCount);
        Assert.Empty(checkpointStore.Items);
        Assert.Empty(queueStore.Items);
    }

    [Fact]
    public async Task HandleAsyncWhenProcessStateIsMissingThrowsInvalidOperationException()
    {
        var existingProcessId = Guid.NewGuid();
        var missingProcessId = Guid.NewGuid();
        var stateStore = new InMemoryBusinessProcessStateStore(CreateInterruptedState(existingProcessId, retryCount: 0, maxRetryCount: 2));
        var checkpointStore = new InMemoryProcessCheckpointStore();
        var queueStore = new InMemoryProcessQueueStore();
        var handler = new TimeoutResumeOnRestartWorkItemHandler(stateStore, checkpointStore, queueStore, TimeProvider.System);

        var queueItem = CreateTimeoutQueueItem(missingProcessId, queueItemId: 508);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(queueItem, "lease-owner-1", CancellationToken.None));

        Assert.Empty(checkpointStore.Items);
        Assert.Empty(queueStore.Items);
    }

    [Fact]
    public async Task HandleAsyncUsesTimeoutSpecificResumeDedupeKeyAcrossRecoveryCycles()
    {
        var processId = Guid.NewGuid();
        var stateStore = new InMemoryBusinessProcessStateStore(CreateInterruptedState(processId, retryCount: 0, maxRetryCount: 2));
        var checkpointStore = new InMemoryProcessCheckpointStore();
        var queueStore = new InMemoryProcessQueueStore();
        var handler = new TimeoutResumeOnRestartWorkItemHandler(stateStore, checkpointStore, queueStore, TimeProvider.System);

        var firstTimeoutItem = CreateTimeoutQueueItem(processId, queueItemId: 1001);
        await handler.HandleAsync(firstTimeoutItem, "lease-owner-1", CancellationToken.None);

        stateStore.State.Status = "interrupted";
        stateStore.State.CurrentStepName = "sync_deadline_exceeded";
        stateStore.State.InterruptedAt = TimeProvider.System.GetUtcNow();

        var secondTimeoutItem = CreateTimeoutQueueItem(processId, queueItemId: 1002);
        await handler.HandleAsync(secondTimeoutItem, "lease-owner-1", CancellationToken.None);

        Assert.Equal(2, queueStore.Items.Count);
        Assert.Equal(2, checkpointStore.Items.Count(checkpoint => checkpoint.CheckpointKind == "retry_scheduled"));

        Assert.Contains(queueStore.Items, item => item.DedupeKey == $"{processId:N}:resume:timeout:1001");
        Assert.Contains(queueStore.Items, item => item.DedupeKey == $"{processId:N}:resume:timeout:1002");
    }

    [Fact]
    public async Task HandleAsyncUsesTimeoutSpecificRetryExhaustedIdempotencyAcrossRecoveryCycles()
    {
        var processId = Guid.NewGuid();
        var stateStore = new InMemoryBusinessProcessStateStore(CreateInterruptedState(processId, retryCount: 3, maxRetryCount: 3));
        var checkpointStore = new InMemoryProcessCheckpointStore();
        var queueStore = new InMemoryProcessQueueStore();
        var handler = new TimeoutResumeOnRestartWorkItemHandler(stateStore, checkpointStore, queueStore, TimeProvider.System);

        var firstTimeoutItem = CreateTimeoutQueueItem(processId, queueItemId: 2001);
        await handler.HandleAsync(firstTimeoutItem, "lease-owner-1", CancellationToken.None);

        stateStore.State.Status = "interrupted";
        stateStore.State.CurrentStepName = "sync_deadline_exceeded";
        stateStore.State.InterruptedAt = TimeProvider.System.GetUtcNow();
        stateStore.State.RetryCount = stateStore.State.MaxRetryCount;

        var secondTimeoutItem = CreateTimeoutQueueItem(processId, queueItemId: 2002);
        await handler.HandleAsync(secondTimeoutItem, "lease-owner-1", CancellationToken.None);

        var pausedCheckpoints = checkpointStore.Items.Where(checkpoint => checkpoint.CheckpointKind == "paused").ToList();

        Assert.Equal(2, pausedCheckpoints.Count);
        Assert.Contains(pausedCheckpoints, checkpoint => checkpoint.IdempotencyKey == $"{processId:N}:resume:retry_exhausted:timeout:2001");
        Assert.Contains(pausedCheckpoints, checkpoint => checkpoint.IdempotencyKey == $"{processId:N}:resume:retry_exhausted:timeout:2002");
    }

    [Fact]
    public async Task HandleAsyncWhenRetryExhaustedTransitionsToPausedAndDoesNotEnqueueResume()
    {
        var processId = Guid.NewGuid();
        var state = CreateInterruptedState(processId, retryCount: 3, maxRetryCount: 3);
        var originalVersion = state.Version;

        var stateStore = new InMemoryBusinessProcessStateStore(state);
        var checkpointStore = new InMemoryProcessCheckpointStore();
        var queueStore = new InMemoryProcessQueueStore();
        var handler = new TimeoutResumeOnRestartWorkItemHandler(stateStore, checkpointStore, queueStore, TimeProvider.System);

        var queueItem = CreateTimeoutQueueItem(processId, queueItemId: 2003);
        await handler.HandleAsync(queueItem, "lease-owner-1", CancellationToken.None);

        Assert.Equal("paused", stateStore.State.Status);
        Assert.True(stateStore.State.AwaitingClientDecision);
        Assert.Equal("resume_retry_exhausted", stateStore.State.LastErrorCode);
        Assert.Contains("retry limit reached", stateStore.State.LastErrorMessage ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(originalVersion + 1, stateStore.State.Version);

        Assert.Single(checkpointStore.Items);
        Assert.Equal("paused", checkpointStore.Items[0].CheckpointKind);
        Assert.Empty(queueStore.Items);
    }

    [Fact]
    public async Task HandleAsyncWhenRetryIsScheduledTransitionsToRetryingAndEnqueuesResume()
    {
        var processId = Guid.NewGuid();
        var state = CreateInterruptedState(processId, retryCount: 1, maxRetryCount: 3);
        state.AwaitingClientDecision = true;
        state.LastErrorCode = "prior_error";
        state.LastErrorMessage = "prior error message";
        var originalVersion = state.Version;
        var originalStepOrdinal = state.CurrentStepOrdinal;

        var stateStore = new InMemoryBusinessProcessStateStore(state);
        var checkpointStore = new InMemoryProcessCheckpointStore();
        var queueStore = new InMemoryProcessQueueStore();
        var handler = new TimeoutResumeOnRestartWorkItemHandler(stateStore, checkpointStore, queueStore, TimeProvider.System);

        var queueItem = CreateTimeoutQueueItem(processId, queueItemId: 2004);
        await handler.HandleAsync(queueItem, "lease-owner-1", CancellationToken.None);

        Assert.Equal("retrying", stateStore.State.Status);
        Assert.Equal("resume_scheduled", stateStore.State.CurrentStepName);
        Assert.Equal(originalStepOrdinal + 1, stateStore.State.CurrentStepOrdinal);
        Assert.Equal(originalVersion + 1, stateStore.State.Version);
        Assert.Equal(2, stateStore.State.RetryCount);
        Assert.False(stateStore.State.AwaitingClientDecision);
        Assert.Null(stateStore.State.ClientDecisionDeadlineAt);
        Assert.Equal("prior_error", stateStore.State.LastErrorCode);
        Assert.Equal("prior error message", stateStore.State.LastErrorMessage);

        Assert.Single(checkpointStore.Items);
        Assert.Equal("retry_scheduled", checkpointStore.Items[0].CheckpointKind);
        Assert.Single(queueStore.Items);
        Assert.Equal("resume", queueStore.Items[0].WorkType);
        Assert.Equal($"{processId:N}:resume:timeout:2004", queueStore.Items[0].DedupeKey);
    }

    [Fact]
    public async Task HandleAsyncWhenReasonMissingUsesResumeOnRestartFallbackInCheckpointAndQueuePayload()
    {
        var processId = Guid.NewGuid();
        var state = CreateInterruptedState(processId, retryCount: 0, maxRetryCount: 3);
        var stateStore = new InMemoryBusinessProcessStateStore(state);
        var checkpointStore = new InMemoryProcessCheckpointStore();
        var queueStore = new InMemoryProcessQueueStore();
        var handler = new TimeoutResumeOnRestartWorkItemHandler(stateStore, checkpointStore, queueStore, TimeProvider.System);

        var queueItem = CreateTimeoutQueueItem(processId, queueItemId: 2005);
        queueItem.Payload = JsonSerializer.Serialize(new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["recoveryPolicy"] = "resume_on_restart",
        });

        await handler.HandleAsync(queueItem, "lease-owner-1", CancellationToken.None);

        Assert.Single(checkpointStore.Items);
        Assert.Single(queueStore.Items);

        using var checkpointData = JsonDocument.Parse(checkpointStore.Items[0].CheckpointData);
        Assert.Equal("resume_on_restart", checkpointData.RootElement.GetProperty("reason").GetString());

        using var queuePayload = JsonDocument.Parse(queueStore.Items[0].Payload);
        Assert.Equal("resume_on_restart", queuePayload.RootElement.GetProperty("reason").GetString());
    }

    [Fact]
    public async Task HandleAsyncWhenReasonProvidedPreservesReasonInCheckpointAndQueuePayload()
    {
        var processId = Guid.NewGuid();
        var state = CreateInterruptedState(processId, retryCount: 0, maxRetryCount: 3);
        var stateStore = new InMemoryBusinessProcessStateStore(state);
        var checkpointStore = new InMemoryProcessCheckpointStore();
        var queueStore = new InMemoryProcessQueueStore();
        var handler = new TimeoutResumeOnRestartWorkItemHandler(stateStore, checkpointStore, queueStore, TimeProvider.System);

        var queueItem = CreateTimeoutQueueItem(processId, queueItemId: 2007);
        queueItem.Payload = JsonSerializer.Serialize(new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["recoveryPolicy"] = "resume_on_restart",
            ["reason"] = "explicit_timeout_reason",
        });

        await handler.HandleAsync(queueItem, "lease-owner-1", CancellationToken.None);

        Assert.Single(checkpointStore.Items);
        Assert.Single(queueStore.Items);

        using var checkpointData = JsonDocument.Parse(checkpointStore.Items[0].CheckpointData);
        Assert.Equal("explicit_timeout_reason", checkpointData.RootElement.GetProperty("reason").GetString());

        using var queuePayload = JsonDocument.Parse(queueStore.Items[0].Payload);
        Assert.Equal("explicit_timeout_reason", queuePayload.RootElement.GetProperty("reason").GetString());
    }

    [Fact]
    public async Task HandleAsyncWhenRetryExhaustedCheckpointCapturesPreviousStateContext()
    {
        var processId = Guid.NewGuid();
        var state = CreateInterruptedState(processId, retryCount: 3, maxRetryCount: 3);
        state.AwaitingClientDecision = true;
        state.ClientDecisionDeadlineAt = TimeProvider.System.GetUtcNow().AddMinutes(10);
        state.LastErrorCode = "prior_code";
        state.LastErrorMessage = "prior error";

        var stateStore = new InMemoryBusinessProcessStateStore(state);
        var checkpointStore = new InMemoryProcessCheckpointStore();
        var queueStore = new InMemoryProcessQueueStore();
        var handler = new TimeoutResumeOnRestartWorkItemHandler(stateStore, checkpointStore, queueStore, TimeProvider.System);

        var queueItem = CreateTimeoutQueueItem(processId, queueItemId: 2006);
        queueItem.AttemptCount = 8;
        queueItem.MaxAttemptCount = 12;
        await handler.HandleAsync(queueItem, "lease-owner-ctx", CancellationToken.None);

        Assert.Single(checkpointStore.Items);
        var checkpoint = checkpointStore.Items[0];
        Assert.Equal("paused", checkpoint.CheckpointKind);

        using var checkpointData = JsonDocument.Parse(checkpoint.CheckpointData);
        Assert.Equal("interrupted", checkpointData.RootElement.GetProperty("previousStatus").GetString());
        Assert.True(checkpointData.RootElement.GetProperty("previousAwaitingClientDecision").GetBoolean());
        Assert.Equal("prior_code", checkpointData.RootElement.GetProperty("previousLastErrorCode").GetString());
        Assert.Equal("prior error", checkpointData.RootElement.GetProperty("previousLastErrorMessage").GetString());
        Assert.Equal(2006, checkpointData.RootElement.GetProperty("triggerQueueItemId").GetInt64());
        Assert.Equal(8, checkpointData.RootElement.GetProperty("triggerAttemptCount").GetInt32());
        Assert.Equal(12, checkpointData.RootElement.GetProperty("triggerMaxAttemptCount").GetInt32());
        Assert.Equal(state.CorrelationId, checkpointData.RootElement.GetProperty("correlationId").GetString());
        Assert.Equal("lease-owner-ctx", checkpointData.RootElement.GetProperty("leaseOwner").GetString());
    }

    [Fact]
    public async Task HandleAsyncWhenRetryExhaustedAndReasonMissingUsesFallbackInPausedCheckpoint()
    {
        var processId = Guid.NewGuid();
        var state = CreateInterruptedState(processId, retryCount: 3, maxRetryCount: 3);
        var stateStore = new InMemoryBusinessProcessStateStore(state);
        var checkpointStore = new InMemoryProcessCheckpointStore();
        var queueStore = new InMemoryProcessQueueStore();
        var handler = new TimeoutResumeOnRestartWorkItemHandler(stateStore, checkpointStore, queueStore, TimeProvider.System);

        var queueItem = CreateTimeoutQueueItem(processId, queueItemId: 2008);
        queueItem.Payload = JsonSerializer.Serialize(new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["recoveryPolicy"] = "resume_on_restart",
        });

        await handler.HandleAsync(queueItem, "lease-owner-1", CancellationToken.None);

        Assert.Single(checkpointStore.Items);
        Assert.Equal("paused", checkpointStore.Items[0].CheckpointKind);

        using var checkpointData = JsonDocument.Parse(checkpointStore.Items[0].CheckpointData);
        Assert.Equal("resume_on_restart", checkpointData.RootElement.GetProperty("reason").GetString());
    }

    [Fact]
    public async Task HandleAsyncWhenRetryExhaustedAndReasonProvidedPreservesInPausedCheckpoint()
    {
        var processId = Guid.NewGuid();
        var state = CreateInterruptedState(processId, retryCount: 3, maxRetryCount: 3);
        var stateStore = new InMemoryBusinessProcessStateStore(state);
        var checkpointStore = new InMemoryProcessCheckpointStore();
        var queueStore = new InMemoryProcessQueueStore();
        var handler = new TimeoutResumeOnRestartWorkItemHandler(stateStore, checkpointStore, queueStore, TimeProvider.System);

        var queueItem = CreateTimeoutQueueItem(processId, queueItemId: 2009);
        queueItem.Payload = JsonSerializer.Serialize(new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["recoveryPolicy"] = "resume_on_restart",
            ["reason"] = "explicit_exhausted_reason",
        });

        await handler.HandleAsync(queueItem, "lease-owner-1", CancellationToken.None);

        Assert.Single(checkpointStore.Items);
        Assert.Equal("paused", checkpointStore.Items[0].CheckpointKind);

        using var checkpointData = JsonDocument.Parse(checkpointStore.Items[0].CheckpointData);
        Assert.Equal("explicit_exhausted_reason", checkpointData.RootElement.GetProperty("reason").GetString());
    }

    [Fact]
    public async Task HandleAsyncWhenRetryScheduledPersistsTriggerAttemptMetadataInCheckpointAndQueuePayload()
    {
        var processId = Guid.NewGuid();
        var state = CreateInterruptedState(processId, retryCount: 0, maxRetryCount: 3);
        var stateStore = new InMemoryBusinessProcessStateStore(state);
        var checkpointStore = new InMemoryProcessCheckpointStore();
        var queueStore = new InMemoryProcessQueueStore();
        var handler = new TimeoutResumeOnRestartWorkItemHandler(stateStore, checkpointStore, queueStore, TimeProvider.System);

        var queueItem = CreateTimeoutQueueItem(processId, queueItemId: 2010);
        queueItem.AttemptCount = 4;
        queueItem.MaxAttemptCount = 9;

        await handler.HandleAsync(queueItem, "lease-owner-1", CancellationToken.None);

        Assert.Single(checkpointStore.Items);
        Assert.Single(queueStore.Items);

        using var checkpointData = JsonDocument.Parse(checkpointStore.Items[0].CheckpointData);
        Assert.Equal(4, checkpointData.RootElement.GetProperty("triggerAttemptCount").GetInt32());
        Assert.Equal(9, checkpointData.RootElement.GetProperty("triggerMaxAttemptCount").GetInt32());
        Assert.Equal(state.CorrelationId, checkpointData.RootElement.GetProperty("correlationId").GetString());

        using var queuePayload = JsonDocument.Parse(queueStore.Items[0].Payload);
        Assert.Equal(4, queuePayload.RootElement.GetProperty("triggerAttemptCount").GetInt32());
        Assert.Equal(9, queuePayload.RootElement.GetProperty("triggerMaxAttemptCount").GetInt32());
        Assert.Equal(state.CorrelationId, queuePayload.RootElement.GetProperty("correlationId").GetString());
        Assert.Equal("resume_on_restart", queuePayload.RootElement.GetProperty("recoveryPolicy").GetString());
    }

    private static BusinessProcessStateModel CreateInterruptedState(Guid processId, int retryCount, int maxRetryCount)
    {
        var now = TimeProvider.System.GetUtcNow();

        return new BusinessProcessStateModel
        {
            ProcessId = processId,
            ProcessName = "trade_sync_flow",
            ProcessKey = "trade:account-1",
            FlowType = "synchronous",
            Status = "interrupted",
            CurrentStepOrdinal = 5,
            CurrentStepName = "sync_deadline_exceeded",
            Version = 2,
            IdempotencyKey = $"{processId:N}:idem",
            CorrelationId = processId.ToString("N"),
            IntentPersistedAt = now,
            RecoveryPolicy = "resume_on_restart",
            RetryCount = retryCount,
            MaxRetryCount = maxRetryCount,
            NextVisibleAt = now,
            InterruptedAt = now,
            ProcessContext = "{}",
            InputPayload = "{}",
            ResultPayload = "{}",
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    private static ProcessQueueItemModel CreateTimeoutQueueItem(Guid processId, long queueItemId)
    {
        var now = TimeProvider.System.GetUtcNow();

        return new ProcessQueueItemModel
        {
            Id = queueItemId,
            ProcessId = processId,
            ProcessKey = "trade:account-1",
            FlowType = "synchronous",
            WorkType = "timeout",
            Status = "ready",
            Priority = 100,
            VisibleAt = now,
            SequenceNo = queueItemId,
            AttemptCount = 1,
            MaxAttemptCount = 5,
            DedupeKey = $"timeout:{queueItemId}",
            Payload = JsonSerializer.Serialize(new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["recoveryPolicy"] = "resume_on_restart",
                ["reason"] = "sync_deadline_exceeded",
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

    private sealed class InMemoryProcessQueueStore : IProcessQueueStore
    {
        public List<ProcessQueueItemModel> Items { get; } = [];

        public Task AddAsync(ProcessQueueItemModel queueItem, CancellationToken cancellationToken = default)
        {
            Items.Add(queueItem);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ProcessQueueItemModel queueItem, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<ProcessQueueItemModel?> FindByDedupeKeyAsync(string dedupeKey, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Items.SingleOrDefault(item => string.Equals(item.DedupeKey, dedupeKey, StringComparison.Ordinal)));
        }

        public Task<IReadOnlyCollection<ProcessQueueItemModel>> LeaseReadyAsync(int batchSize, DateTimeOffset asOfUtc, string leaseOwner, TimeSpan leaseDuration, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<bool> RenewLeaseAsync(long queueItemId, DateTimeOffset asOfUtc, string leaseOwner, TimeSpan leaseDuration, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<bool> CompleteAsync(long queueItemId, DateTimeOffset asOfUtc, string leaseOwner, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<bool> DeadLetterAsync(long queueItemId, DateTimeOffset asOfUtc, string leaseOwner, string errorCode, string errorMessage, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<bool> RequeueAsync(long queueItemId, DateTimeOffset asOfUtc, DateTimeOffset visibleAt, string leaseOwner, string errorCode, string errorMessage, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyCollection<ProcessQueueItemModel>> ListVisibleAsync(int batchSize, DateTimeOffset asOfUtc, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(1);
        }
    }
}
