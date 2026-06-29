using System.Text.Json;
using BffOrchestratorService.Domain;
using BffOrchestratorService.Domain.Abstractions;
using BffOrchestratorService.Domain.Models;
using BffOrchestratorService.Infrastructure.Abstractions;

namespace BffOrchestratorService.Infrastructure.Services;

public sealed class TimeoutResumeOnRestartWorkItemHandler(
    IBusinessProcessStateStore businessProcessStateStore,
    IProcessCheckpointStore processCheckpointStore,
    IProcessQueueStore processQueueStore,
    TimeProvider timeProvider) : IProcessQueueWorkItemHandler
{
    public bool CanHandle(ProcessQueueItemModel queueItem)
    {
        ArgumentNullException.ThrowIfNull(queueItem);

        return queueItem.WorkType == "timeout"
            && string.Equals(GetPayloadStringValue(queueItem.Payload, "recoveryPolicy"), "resume_on_restart", StringComparison.Ordinal);
    }

    public async Task HandleAsync(ProcessQueueItemModel queueItem, string leaseOwner, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queueItem);
        ArgumentException.ThrowIfNullOrWhiteSpace(leaseOwner);

        var resumeDedupeKey = BuildResumeDedupeKey(queueItem.ProcessId, queueItem.Id);
        var retryScheduledIdempotencyKey = BuildResumeRetryScheduledIdempotencyKey(queueItem.ProcessId, queueItem.Id);
        var existingResumeQueueItem = await processQueueStore.FindByDedupeKeyAsync(resumeDedupeKey, cancellationToken);

        if (existingResumeQueueItem is not null)
        {
            return;
        }

        var processState = await businessProcessStateStore.FindByProcessIdAsync(queueItem.ProcessId, cancellationToken)
            ?? throw new InvalidOperationException($"Business process state '{queueItem.ProcessId}' was not found for timeout recovery.");

        if (IsTerminalStatus(processState.Status))
        {
            return;
        }

        if (!string.Equals(processState.Status, "interrupted", StringComparison.Ordinal))
        {
            return;
        }

        if (!string.Equals(processState.RecoveryPolicy, "resume_on_restart", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Process '{processState.ProcessId}' uses unsupported recovery policy '{processState.RecoveryPolicy}' for timeout resume scheduling.");
        }

        var observedAt = timeProvider.GetUtcNow();
        var previousStatus = processState.Status;
        var previousStepOrdinal = processState.CurrentStepOrdinal;
        var previousStepName = processState.CurrentStepName;
        var previousInterruptedAt = processState.InterruptedAt;
        var previousRetryCount = processState.RetryCount;
        var previousAwaitingClientDecision = processState.AwaitingClientDecision;
        var previousClientDecisionDeadlineAt = processState.ClientDecisionDeadlineAt;
        var previousLastErrorCode = processState.LastErrorCode;
        var previousLastErrorMessage = processState.LastErrorMessage;
        var resumeReason = GetPayloadStringValue(queueItem.Payload, "reason") ?? "resume_on_restart";
        var retryExhaustedIdempotencyKey = BuildResumeRetryExhaustedIdempotencyKey(processState.ProcessId, queueItem.Id);
        processState.InterruptedAt ??= observedAt;

        if (processState.RetryCount >= processState.MaxRetryCount)
        {
            var retryExhaustedCheckpointExists = await processCheckpointStore.ExistsAsync(
                processState.ProcessId,
                "paused",
                retryExhaustedIdempotencyKey,
                cancellationToken);

            if (retryExhaustedCheckpointExists)
            {
                return;
            }

            var exhaustedStepOrdinal = processState.CurrentStepOrdinal + 1;
            processState.Status = "paused";
            processState.CurrentStepOrdinal = exhaustedStepOrdinal;
            processState.CurrentStepName = "resume_retry_exhausted";
            processState.Version += 1;
            processState.AwaitingClientDecision = true;
            processState.ClientDecisionDeadlineAt = null;
            processState.NextVisibleAt = observedAt;
            processState.LastErrorCode = "resume_retry_exhausted";
            processState.LastErrorMessage =
                $"Resume-on-restart retry limit reached ({processState.RetryCount}/{processState.MaxRetryCount}).";
            processState.UpdatedAt = observedAt;

            var retryExhaustedCheckpoint = new ProcessCheckpointModel
            {
                ProcessId = processState.ProcessId,
                StepOrdinal = exhaustedStepOrdinal,
                StepName = "resume",
                CheckpointKind = "paused",
                ObservedOutcome = "paused",
                DispatchId = Guid.NewGuid(),
                IdempotencyKey = retryExhaustedIdempotencyKey,
                CheckpointData = JsonSerializer.Serialize(new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["triggerQueueItemId"] = queueItem.Id,
                    ["triggerWorkType"] = queueItem.WorkType,
                    ["triggerAttemptCount"] = queueItem.AttemptCount,
                    ["triggerMaxAttemptCount"] = queueItem.MaxAttemptCount,
                    ["correlationId"] = processState.CorrelationId,
                    ["reason"] = resumeReason,
                    ["previousStatus"] = previousStatus,
                    ["previousStepOrdinal"] = previousStepOrdinal,
                    ["previousStepName"] = previousStepName,
                    ["previousInterruptedAt"] = previousInterruptedAt,
                    ["previousRetryCount"] = previousRetryCount,
                    ["previousAwaitingClientDecision"] = previousAwaitingClientDecision,
                    ["previousClientDecisionDeadlineAt"] = previousClientDecisionDeadlineAt,
                    ["previousLastErrorCode"] = previousLastErrorCode,
                    ["previousLastErrorMessage"] = previousLastErrorMessage,
                    ["retryCount"] = processState.RetryCount,
                    ["maxRetryCount"] = processState.MaxRetryCount,
                    ["leaseOwner"] = leaseOwner,
                }),
                OccurredAt = observedAt,
                CreatedAt = observedAt,
            };

            await businessProcessStateStore.UpdateAsync(processState, cancellationToken);
            await processCheckpointStore.AddAsync(retryExhaustedCheckpoint, cancellationToken);
            await processQueueStore.SaveChangesAsync(cancellationToken);
            return;
        }

        var retryScheduledCheckpointExists = await processCheckpointStore.ExistsAsync(
            processState.ProcessId,
            "retry_scheduled",
            retryScheduledIdempotencyKey,
            cancellationToken);

        if (retryScheduledCheckpointExists)
        {
            return;
        }

        var scheduledStepOrdinal = processState.CurrentStepOrdinal + 1;

        processState.Status = "retrying";
        processState.CurrentStepOrdinal = scheduledStepOrdinal;
        processState.CurrentStepName = "resume_scheduled";
        processState.Version += 1;
        processState.RetryCount += 1;
        processState.AwaitingClientDecision = false;
        processState.ClientDecisionDeadlineAt = null;
        processState.NextVisibleAt = observedAt;
        processState.UpdatedAt = observedAt;

        var retryScheduledCheckpoint = new ProcessCheckpointModel
        {
            ProcessId = processState.ProcessId,
            StepOrdinal = scheduledStepOrdinal,
            StepName = "resume",
            CheckpointKind = "retry_scheduled",
            ObservedOutcome = "pending",
            DispatchId = Guid.NewGuid(),
            IdempotencyKey = retryScheduledIdempotencyKey,
            CheckpointData = JsonSerializer.Serialize(new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["triggerQueueItemId"] = queueItem.Id,
                ["triggerWorkType"] = queueItem.WorkType,
                ["triggerAttemptCount"] = queueItem.AttemptCount,
                ["triggerMaxAttemptCount"] = queueItem.MaxAttemptCount,
                ["correlationId"] = processState.CorrelationId,
                ["scheduledWorkType"] = "resume",
                ["leaseOwner"] = leaseOwner,
                ["reason"] = resumeReason,
                ["previousStatus"] = previousStatus,
                ["previousStepOrdinal"] = previousStepOrdinal,
                ["previousStepName"] = previousStepName,
                ["previousInterruptedAt"] = previousInterruptedAt,
                ["previousRetryCount"] = previousRetryCount,
                ["previousAwaitingClientDecision"] = previousAwaitingClientDecision,
                ["previousClientDecisionDeadlineAt"] = previousClientDecisionDeadlineAt,
                ["previousLastErrorCode"] = previousLastErrorCode,
                ["previousLastErrorMessage"] = previousLastErrorMessage,
                ["retryCount"] = processState.RetryCount,
                ["maxRetryCount"] = processState.MaxRetryCount,
            }),
            OccurredAt = observedAt,
            CreatedAt = observedAt,
        };

        var resumeQueueItem = new ProcessQueueItemModel
        {
            ProcessId = processState.ProcessId,
            ProcessKey = processState.ProcessKey,
            FlowType = processState.FlowType,
            WorkType = "resume",
            Status = "ready",
            Priority = queueItem.Priority,
            VisibleAt = observedAt,
            AttemptCount = 0,
            MaxAttemptCount = queueItem.MaxAttemptCount,
            DedupeKey = resumeDedupeKey,
            Payload = JsonSerializer.Serialize(new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["triggerQueueItemId"] = queueItem.Id,
                ["triggerAttemptCount"] = queueItem.AttemptCount,
                ["triggerMaxAttemptCount"] = queueItem.MaxAttemptCount,
                ["processId"] = processState.ProcessId,
                ["processName"] = processState.ProcessName,
                ["correlationId"] = processState.CorrelationId,
                ["recoveryPolicy"] = processState.RecoveryPolicy,
                ["reason"] = resumeReason,
            }),
            CreatedAt = observedAt,
            UpdatedAt = observedAt,
        };

        await businessProcessStateStore.UpdateAsync(processState, cancellationToken);
        await processCheckpointStore.AddAsync(retryScheduledCheckpoint, cancellationToken);
        await processQueueStore.AddAsync(resumeQueueItem, cancellationToken);
        await processQueueStore.SaveChangesAsync(cancellationToken);
    }

    private static string BuildResumeDedupeKey(Guid processId, long timeoutQueueItemId)
    {
        return $"{processId:N}:resume:timeout:{timeoutQueueItemId}";
    }

    private static string BuildResumeRetryScheduledIdempotencyKey(Guid processId, long timeoutQueueItemId)
    {
        return $"{processId:N}:resume:timeout:{timeoutQueueItemId}";
    }

    private static string BuildResumeRetryExhaustedIdempotencyKey(Guid processId, long timeoutQueueItemId)
    {
        return $"{processId:N}:resume:retry_exhausted:timeout:{timeoutQueueItemId}";
    }

    private static bool IsTerminalStatus(string status)
    {
        return status is "completed" or "failed";
    }

    private static string? GetPayloadStringValue(string payload, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(payload);

            return document.RootElement.TryGetProperty(propertyName, out var property)
                && property.ValueKind == JsonValueKind.String
                    ? property.GetString()
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}