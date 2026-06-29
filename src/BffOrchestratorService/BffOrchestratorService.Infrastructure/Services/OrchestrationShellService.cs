using System.Collections.Concurrent;
using System.Text.Json;
using BffOrchestratorService.Domain;
using BffOrchestratorService.Domain.Abstractions;
using BffOrchestratorService.Domain.Entities;
using BffOrchestratorService.Domain.Models;
using BffOrchestratorService.Domain.Services;
using BffOrchestratorService.Infrastructure.Abstractions;

namespace BffOrchestratorService.Infrastructure.Services;

public sealed class OrchestrationShellService(
    IEnumerable<IDownstreamOrchestrationProbeClient> downstreamProbeClients,
    IBusinessProcessStateStore businessProcessStateStore,
    IProcessCheckpointStore processCheckpointStore,
    IProcessQueueStore processQueueStore,
    IOrchestrationShellStore orchestrationShellStore,
    OrchestrationShellDomainService orchestrationShellDomainService,
    TimeProvider timeProvider) : IOrchestrationShellService
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> ProcessKeyGates = new(StringComparer.Ordinal);
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan DefaultSyncDeadline = TimeSpan.FromSeconds(30);

    public async Task<OrchestrationShell> StartAsync(
        string flowName,
        string correlationId,
        string idempotencyKey,
        string requestedBySubjectId,
        string requestedByUserName,
        IReadOnlyCollection<string>? involvedServices = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(flowName);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedBySubjectId);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedByUserName);

        var existingProcessState = await businessProcessStateStore.FindByProcessNameAndIdempotencyKeyAsync(
            flowName,
            idempotencyKey,
            cancellationToken);

        if (existingProcessState is not null)
        {
            return await GetOrRestoreAsync(existingProcessState, cancellationToken);
        }

        var processKey = BuildProcessKey(flowName, requestedBySubjectId);

        return await ExecuteSerializedByProcessKeyAsync(
            processKey,
            async ct =>
            {
                var replayProcessState = await businessProcessStateStore.FindByProcessNameAndIdempotencyKeyAsync(
                    flowName,
                    idempotencyKey,
                    ct);

                if (replayProcessState is not null)
                {
                    return await GetOrRestoreAsync(replayProcessState, ct);
                }

                var activeProcessState = await businessProcessStateStore.FindActiveByProcessKeyAsync(
                    processKey,
                    timeProvider.GetUtcNow(),
                    ct);

                if (activeProcessState is not null)
                {
                    return await GetOrRestoreAsync(activeProcessState, ct);
                }

                var createdAtUtc = timeProvider.GetUtcNow();
                var processState = new Domain.Models.BusinessProcessStateModel
                {
                    ProcessId = Guid.NewGuid(),
                    ProcessName = flowName,
                    ProcessKey = processKey,
                    FlowType = "synchronous",
                    Status = "started",
                    CurrentStepOrdinal = 0,
                    CurrentStepName = "intent_persisted",
                    Version = 1,
                    IdempotencyKey = idempotencyKey,
                    CorrelationId = correlationId,
                    IntentPersistedAt = createdAtUtc,
                    SyncDeadlineAt = createdAtUtc.Add(DefaultSyncDeadline),
                    RecoveryPolicy = "resume_on_restart",
                    RetryCount = 0,
                    MaxRetryCount = 3,
                    NextVisibleAt = createdAtUtc,
                    InputPayload = SerializeInputDocument(flowName, requestedBySubjectId, requestedByUserName),
                    ProcessContext = SerializeContextDocument(false, Array.Empty<OrchestrationDependencyStatusModel>()),
                    ResultPayload = SerializeResultDocument("Started"),
                    CreatedAt = createdAtUtc,
                    UpdatedAt = createdAtUtc,
                };

                var dependencyProbeDispatchId = Guid.NewGuid();
                var dependencyProbeDispatchCheckpoint = new Domain.Models.ProcessCheckpointModel
                {
                    ProcessId = processState.ProcessId,
                    StepOrdinal = 1,
                    StepName = "dependency_probe",
                    CheckpointKind = "dispatch",
                    ObservedOutcome = "pending",
                    DispatchId = dependencyProbeDispatchId,
                    IdempotencyKey = BuildCheckpointIdempotencyKey(idempotencyKey, "dependency_probe_dispatch"),
                    CheckpointData = SerializeCheckpointData(new Dictionary<string, object?>(StringComparer.Ordinal)
                    {
                        ["correlationId"] = correlationId,
                        ["flowName"] = flowName,
                        ["processKey"] = processKey,
                    }),
                    OccurredAt = createdAtUtc,
                    CreatedAt = createdAtUtc,
                };

                await businessProcessStateStore.AddAsync(processState, ct);
                await processCheckpointStore.AddAsync(dependencyProbeDispatchCheckpoint, ct);
                await businessProcessStateStore.SaveChangesAsync(ct);

                var selectedClients = SelectClients(involvedServices);
                var dependencies = await Task.WhenAll(
                    selectedClients.Select(client => client.GetStatusAsync(correlationId, ct)));

                Array.Sort(dependencies, static (left, right) => string.Compare(left.ServiceName, right.ServiceName, StringComparison.Ordinal));

                var dependenciesHealthy = dependencies.All(static dependency => dependency.IsHealthy);
                processState.Status = dependenciesHealthy ? "in_progress" : "waiting";
                processState.CurrentStepOrdinal = 1;
                processState.CurrentStepName = dependenciesHealthy ? "dependency_probe_succeeded" : "dependency_probe_pending";
                processState.Version += 1;
                processState.ProcessContext = SerializeContextDocument(dependenciesHealthy, dependencies);
                processState.ResultPayload = SerializeResultDocument(dependenciesHealthy ? "Accepted" : "PendingDependencies");
                processState.UpdatedAt = timeProvider.GetUtcNow();

                var dependencyProbeResultCheckpoint = new Domain.Models.ProcessCheckpointModel
                {
                    ProcessId = processState.ProcessId,
                    StepOrdinal = 1,
                    StepName = "dependency_probe",
                    CheckpointKind = dependenciesHealthy ? "validation_succeeded" : "step_failed",
                    ObservedOutcome = dependenciesHealthy ? "validated" : "failed",
                    DispatchId = dependencyProbeDispatchId,
                    IdempotencyKey = BuildCheckpointIdempotencyKey(idempotencyKey, "dependency_probe_result"),
                    CheckpointData = SerializeCheckpointData(new Dictionary<string, object?>(StringComparer.Ordinal)
                    {
                        ["dependenciesHealthy"] = dependenciesHealthy,
                        ["dependencyCount"] = dependencies.Length,
                        ["services"] = dependencies.Select(static dependency => dependency.ServiceName).ToArray(),
                    }),
                    OccurredAt = processState.UpdatedAt,
                    CreatedAt = processState.UpdatedAt,
                };

                await businessProcessStateStore.UpdateAsync(processState, ct);
                await processCheckpointStore.AddAsync(dependencyProbeResultCheckpoint, ct);
                await businessProcessStateStore.SaveChangesAsync(ct);

                if (HasSyncDeadlineExpired(processState.SyncDeadlineAt, processState.UpdatedAt))
                {
                    var timeoutObservedAt = timeProvider.GetUtcNow();
                    processState.Status = "interrupted";
                    processState.CurrentStepOrdinal = 2;
                    processState.CurrentStepName = "sync_deadline_exceeded";
                    processState.Version += 1;
                    processState.InterruptedAt = timeoutObservedAt;
                    processState.NextVisibleAt = timeoutObservedAt;
                    processState.LastErrorCode = "sync_deadline_exceeded";
                    processState.LastErrorMessage = $"Synchronous deadline expired at {processState.SyncDeadlineAt:O}.";
                    processState.ResultPayload = SerializeResultDocument("Interrupted");
                    processState.UpdatedAt = timeoutObservedAt;

                    var timeoutCheckpoint = new Domain.Models.ProcessCheckpointModel
                    {
                        ProcessId = processState.ProcessId,
                        StepOrdinal = 2,
                        StepName = "sync_deadline",
                        CheckpointKind = "timeout",
                        ObservedOutcome = "timed_out",
                        DispatchId = Guid.NewGuid(),
                        IdempotencyKey = BuildCheckpointIdempotencyKey(idempotencyKey, "sync_deadline"),
                        CheckpointData = SerializeCheckpointData(new Dictionary<string, object?>(StringComparer.Ordinal)
                        {
                            ["syncDeadlineAt"] = processState.SyncDeadlineAt,
                            ["observedAt"] = timeoutObservedAt,
                            ["processKey"] = processKey,
                        }),
                        OccurredAt = timeoutObservedAt,
                        CreatedAt = timeoutObservedAt,
                    };

                    var timeoutQueueItem = new Domain.Models.ProcessQueueItemModel
                    {
                        ProcessId = processState.ProcessId,
                        ProcessKey = processState.ProcessKey,
                        FlowType = "synchronous",
                        WorkType = "timeout",
                        Status = "ready",
                        Priority = 10,
                        VisibleAt = timeoutObservedAt,
                        AttemptCount = 0,
                        MaxAttemptCount = 3,
                        DedupeKey = BuildQueueDedupeKey(processState.ProcessId, "timeout"),
                        Payload = SerializeQueuePayload(new Dictionary<string, object?>(StringComparer.Ordinal)
                        {
                            ["processId"] = processState.ProcessId,
                            ["processName"] = processState.ProcessName,
                            ["correlationId"] = processState.CorrelationId,
                            ["recoveryPolicy"] = processState.RecoveryPolicy,
                            ["reason"] = "sync_deadline_exceeded",
                        }),
                        CreatedAt = timeoutObservedAt,
                        UpdatedAt = timeoutObservedAt,
                    };

                    await businessProcessStateStore.UpdateAsync(processState, ct);
                    await processCheckpointStore.AddAsync(timeoutCheckpoint, ct);
                    await processQueueStore.AddAsync(timeoutQueueItem, ct);
                    await businessProcessStateStore.SaveChangesAsync(ct);
                }

                var orchestrationShell = RestoreShell(processState);

                await orchestrationShellStore.AddAsync(orchestrationShell, ct);
                return orchestrationShell;
            },
            cancellationToken);
    }

    public async Task<OrchestrationShell?> GetAsync(Guid orchestrationShellId, CancellationToken cancellationToken = default)
    {
        var orchestrationShell = await orchestrationShellStore.GetAsync(orchestrationShellId, cancellationToken);

        if (orchestrationShell is not null)
        {
            return orchestrationShell;
        }

        var processState = await businessProcessStateStore.FindByProcessIdAsync(orchestrationShellId, cancellationToken);
        return processState is null ? null : RestoreShell(processState);
    }

    private async Task<OrchestrationShell> GetOrRestoreAsync(Domain.Models.BusinessProcessStateModel processState, CancellationToken cancellationToken)
    {
        var orchestrationShell = await orchestrationShellStore.GetAsync(processState.ProcessId, cancellationToken);
        return orchestrationShell ?? RestoreShell(processState);
    }

    private OrchestrationShell RestoreShell(Domain.Models.BusinessProcessStateModel processState)
    {
        var inputDocument = DeserializeInputDocument(processState.InputPayload);
        var contextDocument = DeserializeContextDocument(processState.ProcessContext);

        return orchestrationShellDomainService.Restore(
            processState.ProcessId,
            processState.ProcessName,
            processState.CorrelationId,
            inputDocument.RequestedBySubjectId,
            inputDocument.RequestedByUserName,
            GetShellStatus(processState.Status, contextDocument.DependenciesHealthy),
            contextDocument.DependenciesHealthy,
            contextDocument.Dependencies,
            processState.Version,
            processState.CreatedAt,
            processState.UpdatedAt);
    }

    private static string BuildProcessKey(string flowName, string requestedBySubjectId)
    {
        return $"{flowName.Trim()}:{requestedBySubjectId.Trim()}";
    }

    private static string BuildCheckpointIdempotencyKey(string idempotencyKey, string stepKey)
    {
        return $"{idempotencyKey.Trim()}:{stepKey}";
    }

    private static string BuildQueueDedupeKey(Guid processId, string workType)
    {
        return $"{processId:N}:{workType.Trim()}";
    }

    private static async Task<OrchestrationShell> ExecuteSerializedByProcessKeyAsync(
        string processKey,
        Func<CancellationToken, Task<OrchestrationShell>> executeAsync,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(processKey);
        ArgumentNullException.ThrowIfNull(executeAsync);

        var gate = ProcessKeyGates.GetOrAdd(processKey, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);

        try
        {
            return await executeAsync(cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    private static bool HasSyncDeadlineExpired(DateTimeOffset? syncDeadlineAt, DateTimeOffset observedAtUtc)
    {
        return syncDeadlineAt.HasValue && observedAtUtc >= syncDeadlineAt.Value;
    }

    private IDownstreamOrchestrationProbeClient[] SelectClients(IReadOnlyCollection<string>? involvedServices)
    {
        if (involvedServices is null || involvedServices.Count == 0)
        {
            return downstreamProbeClients.ToArray();
        }

        var requested = new HashSet<string>(involvedServices, StringComparer.OrdinalIgnoreCase);
        var matched = downstreamProbeClients
            .Where(client => requested.Contains(client.ServiceName))
            .ToArray();

        return matched.Length == 0 ? downstreamProbeClients.ToArray() : matched;
    }

    private static string GetShellStatus(string processStatus, bool dependenciesHealthy)
    {
        return processStatus switch
        {
            "started" => "Started",
            "waiting" when !dependenciesHealthy => "PendingDependencies",
            "in_progress" when dependenciesHealthy => "Accepted",
            "completed" => "Completed",
            "failed" => "Failed",
            "interrupted" => "Interrupted",
            "paused" => "Paused",
            _ => processStatus,
        };
    }

    private string SerializeInputDocument(string flowName, string requestedBySubjectId, string requestedByUserName)
    {
        return JsonSerializer.Serialize(
            new ProcessInputDocument(flowName, requestedBySubjectId, requestedByUserName),
            SerializerOptions);
    }

    private ProcessInputDocument DeserializeInputDocument(string inputPayload)
    {
        return JsonSerializer.Deserialize<ProcessInputDocument>(inputPayload, SerializerOptions)
            ?? ProcessInputDocument.Empty;
    }

    private string SerializeContextDocument(bool dependenciesHealthy, OrchestrationDependencyStatusModel[] dependencies)
    {
        return JsonSerializer.Serialize(
            new ProcessContextDocument(dependenciesHealthy, dependencies),
            SerializerOptions);
    }

    private ProcessContextDocument DeserializeContextDocument(string processContext)
    {
        return JsonSerializer.Deserialize<ProcessContextDocument>(processContext, SerializerOptions)
            ?? ProcessContextDocument.Empty;
    }

    private string SerializeResultDocument(string status)
    {
        return JsonSerializer.Serialize(new ProcessResultDocument(status), SerializerOptions);
    }

    private string SerializeCheckpointData(IReadOnlyDictionary<string, object?> checkpointData)
    {
        return JsonSerializer.Serialize(checkpointData, SerializerOptions);
    }

    private string SerializeQueuePayload(IReadOnlyDictionary<string, object?> queuePayload)
    {
        return JsonSerializer.Serialize(queuePayload, SerializerOptions);
    }

    private sealed record ProcessInputDocument(string FlowName, string RequestedBySubjectId, string RequestedByUserName)
    {
        public static readonly ProcessInputDocument Empty = new(string.Empty, "anonymous-subject", "anonymous-user");
    }

    private sealed record ProcessContextDocument(bool DependenciesHealthy, OrchestrationDependencyStatusModel[] Dependencies)
    {
        public static readonly ProcessContextDocument Empty = new(false, Array.Empty<OrchestrationDependencyStatusModel>());
    }

    private sealed record ProcessResultDocument(string Status);
}