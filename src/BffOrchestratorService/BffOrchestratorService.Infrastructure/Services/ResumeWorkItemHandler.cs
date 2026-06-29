using System.Text.Json;
using BffOrchestratorService.Domain;
using BffOrchestratorService.Domain.Abstractions;
using BffOrchestratorService.Domain.Models;
using BffOrchestratorService.Infrastructure.Abstractions;

namespace BffOrchestratorService.Infrastructure.Services;

public sealed class ResumeWorkItemHandler(
    IBusinessProcessStateStore businessProcessStateStore,
    IProcessCheckpointStore processCheckpointStore,
    TimeProvider timeProvider) : IProcessQueueWorkItemHandler
{
    public bool CanHandle(ProcessQueueItemModel queueItem)
    {
        ArgumentNullException.ThrowIfNull(queueItem);

        return queueItem.WorkType == "resume"
            && string.Equals(GetPayloadStringValue(queueItem.Payload, "recoveryPolicy"), "resume_on_restart", StringComparison.Ordinal);
    }

    public async Task HandleAsync(ProcessQueueItemModel queueItem, string leaseOwner, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(queueItem);
        ArgumentException.ThrowIfNullOrWhiteSpace(leaseOwner);

        var resumeHandledIdempotencyKey = BuildResumeHandledIdempotencyKey(queueItem.ProcessId, queueItem.Id);
        var checkpointExists = await processCheckpointStore.ExistsAsync(
            queueItem.ProcessId,
            "resumed",
            resumeHandledIdempotencyKey,
            cancellationToken);

        if (checkpointExists)
        {
            return;
        }

        var processState = await businessProcessStateStore.FindByProcessIdAsync(queueItem.ProcessId, cancellationToken)
            ?? throw new InvalidOperationException($"Business process state '{queueItem.ProcessId}' was not found for resume handling.");

        if (IsTerminalStatus(processState.Status))
        {
            return;
        }

        if (!string.Equals(processState.Status, "interrupted", StringComparison.Ordinal)
            && !string.Equals(processState.Status, "retrying", StringComparison.Ordinal))
        {
            return;
        }

        if (!string.Equals(processState.RecoveryPolicy, "resume_on_restart", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Process '{processState.ProcessId}' uses unsupported recovery policy '{processState.RecoveryPolicy}' for resume handling.");
        }

        var resumedAt = timeProvider.GetUtcNow();
        var previousStatus = processState.Status;
        var previousStepOrdinal = processState.CurrentStepOrdinal;
        var previousStepName = processState.CurrentStepName;
        var previousInterruptedAt = processState.InterruptedAt;
        var previousAwaitingClientDecision = processState.AwaitingClientDecision;
        var previousClientDecisionDeadlineAt = processState.ClientDecisionDeadlineAt;
        var previousRetryCount = processState.RetryCount;
        var previousLastErrorCode = processState.LastErrorCode;
        var previousLastErrorMessage = processState.LastErrorMessage;
        var resumedStepOrdinal = processState.CurrentStepOrdinal + 1;

        processState.Status = "waiting";
        processState.CurrentStepOrdinal = resumedStepOrdinal;
        processState.CurrentStepName = "resumed";
        processState.Version += 1;
        processState.AwaitingClientDecision = false;
        processState.ClientDecisionDeadlineAt = null;
        processState.NextVisibleAt = resumedAt;
        processState.InterruptedAt = null;
        processState.LastErrorCode = null;
        processState.LastErrorMessage = null;
        processState.UpdatedAt = resumedAt;

        var resumedCheckpoint = new ProcessCheckpointModel
        {
            ProcessId = processState.ProcessId,
            StepOrdinal = resumedStepOrdinal,
            StepName = "resume",
            CheckpointKind = "resumed",
            ObservedOutcome = "pending",
            DispatchId = Guid.NewGuid(),
            IdempotencyKey = resumeHandledIdempotencyKey,
            CheckpointData = JsonSerializer.Serialize(new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["triggerQueueItemId"] = queueItem.Id,
                ["triggerWorkType"] = queueItem.WorkType,
                ["triggerAttemptCount"] = queueItem.AttemptCount,
                ["triggerMaxAttemptCount"] = queueItem.MaxAttemptCount,
                ["leaseOwner"] = leaseOwner,
                ["reason"] = GetPayloadStringValue(queueItem.Payload, "reason") ?? "resume_on_restart",
                ["correlationId"] = processState.CorrelationId,
                ["recoveryPolicy"] = processState.RecoveryPolicy,
                ["previousStatus"] = previousStatus,
                ["previousStepOrdinal"] = previousStepOrdinal,
                ["previousStepName"] = previousStepName,
                ["previousInterruptedAt"] = previousInterruptedAt,
                ["previousAwaitingClientDecision"] = previousAwaitingClientDecision,
                ["previousClientDecisionDeadlineAt"] = previousClientDecisionDeadlineAt,
                ["previousRetryCount"] = previousRetryCount,
                ["previousLastErrorCode"] = previousLastErrorCode,
                ["previousLastErrorMessage"] = previousLastErrorMessage,
                ["retryCount"] = processState.RetryCount,
                ["maxRetryCount"] = processState.MaxRetryCount,
            }),
            OccurredAt = resumedAt,
            CreatedAt = resumedAt,
        };

        await businessProcessStateStore.UpdateAsync(processState, cancellationToken);
        await processCheckpointStore.AddAsync(resumedCheckpoint, cancellationToken);
        await businessProcessStateStore.SaveChangesAsync(cancellationToken);
    }

    private static string BuildResumeHandledIdempotencyKey(Guid processId, long resumeQueueItemId)
    {
        return $"{processId:N}:resume:handled:{resumeQueueItemId}";
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