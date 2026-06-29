using System.Text.Json;
using BffOrchestratorService.Domain.Abstractions;
using BffOrchestratorService.Domain.Entities;
using BffOrchestratorService.Domain.Models;
using BffOrchestratorService.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace BffOrchestratorService.Persistence.Stores;

public sealed class DatabaseOrchestrationShellStore(
    BffOrchestratorDbContext dbContext,
    OrchestrationShellDomainService orchestrationShellDomainService) : IOrchestrationShellStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task AddAsync(OrchestrationShell orchestrationShell, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(orchestrationShell);

        var entity = await dbContext.BusinessProcessStates
            .SingleOrDefaultAsync(state => state.Id == orchestrationShell.Id, cancellationToken);

        if (entity is null)
        {
            entity = new BusinessProcessState
            {
                Id = orchestrationShell.Id,
                ProcessName = orchestrationShell.FlowName,
                ProcessKey = BuildProcessKey(orchestrationShell.FlowName, orchestrationShell.RequestedBySubjectId),
                FlowType = "synchronous",
                Status = ToProcessStatus(orchestrationShell.Status, orchestrationShell.DependenciesHealthy),
                CurrentStepOrdinal = 1,
                CurrentStepName = orchestrationShell.DependenciesHealthy ? "dependency_probe_succeeded" : "dependency_probe_pending",
                Version = orchestrationShell.Version,
                IdempotencyKey = orchestrationShell.CorrelationId,
                CorrelationId = orchestrationShell.CorrelationId,
                IntentPersistedAt = orchestrationShell.CreatedAt,
                RecoveryPolicy = "resume_on_restart",
                NextVisibleAt = orchestrationShell.UpdatedAt,
                ProcessContext = SerializeContextDocument(orchestrationShell.DependenciesHealthy, orchestrationShell.Dependencies),
                InputPayload = SerializeInputDocument(orchestrationShell.FlowName, orchestrationShell.RequestedBySubjectId, orchestrationShell.RequestedByUserName),
                ResultPayload = SerializeResultDocument(orchestrationShell.Status),
                CreatedAt = orchestrationShell.CreatedAt,
                UpdatedAt = orchestrationShell.UpdatedAt,
            };

            await dbContext.BusinessProcessStates.AddAsync(entity, cancellationToken);
        }
        else
        {
            entity.Status = ToProcessStatus(orchestrationShell.Status, orchestrationShell.DependenciesHealthy);
            entity.Version = orchestrationShell.Version;
            entity.ProcessContext = SerializeContextDocument(orchestrationShell.DependenciesHealthy, orchestrationShell.Dependencies);
            entity.ResultPayload = SerializeResultDocument(orchestrationShell.Status);
            entity.UpdatedAt = orchestrationShell.UpdatedAt;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<OrchestrationShell?> GetAsync(Guid orchestrationShellId, CancellationToken cancellationToken = default)
    {
        var processState = await dbContext.BusinessProcessStates
            .AsNoTracking()
            .SingleOrDefaultAsync(state => state.Id == orchestrationShellId, cancellationToken);

        if (processState is null)
        {
            return null;
        }

        var inputDocument = DeserializeInputDocument(processState.InputPayload);
        var contextDocument = DeserializeContextDocument(processState.ProcessContext);

        return orchestrationShellDomainService.Restore(
            processState.Id,
            processState.ProcessName,
            processState.CorrelationId,
            inputDocument.RequestedBySubjectId,
            inputDocument.RequestedByUserName,
            ToShellStatus(processState.Status, contextDocument.DependenciesHealthy),
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

    private static string ToProcessStatus(string shellStatus, bool dependenciesHealthy)
    {
        return shellStatus switch
        {
            "Started" => "started",
            "PendingDependencies" => "waiting",
            "Accepted" => dependenciesHealthy ? "in_progress" : "waiting",
            "Completed" => "completed",
            "Failed" => "failed",
            "Interrupted" => "interrupted",
            "Paused" => "paused",
            _ => shellStatus.ToLowerInvariant(),
        };
    }

    private static string ToShellStatus(string processStatus, bool dependenciesHealthy)
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

    private static string SerializeInputDocument(string flowName, string requestedBySubjectId, string requestedByUserName)
    {
        return JsonSerializer.Serialize(
            new ProcessInputDocument(flowName, requestedBySubjectId, requestedByUserName),
            SerializerOptions);
    }

    private static ProcessInputDocument DeserializeInputDocument(string inputPayload)
    {
        return JsonSerializer.Deserialize<ProcessInputDocument>(inputPayload, SerializerOptions)
            ?? ProcessInputDocument.Empty;
    }

    private static string SerializeContextDocument(bool dependenciesHealthy, IReadOnlyCollection<OrchestrationDependencyStatusModel> dependencies)
    {
        return JsonSerializer.Serialize(
            new ProcessContextDocument(dependenciesHealthy, dependencies.ToArray()),
            SerializerOptions);
    }

    private static ProcessContextDocument DeserializeContextDocument(string processContext)
    {
        return JsonSerializer.Deserialize<ProcessContextDocument>(processContext, SerializerOptions)
            ?? ProcessContextDocument.Empty;
    }

    private static string SerializeResultDocument(string status)
    {
        return JsonSerializer.Serialize(new ProcessResultDocument(status), SerializerOptions);
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
