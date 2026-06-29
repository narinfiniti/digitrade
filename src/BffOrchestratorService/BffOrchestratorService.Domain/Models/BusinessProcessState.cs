namespace BffOrchestratorService.Domain.Models;

public sealed class BusinessProcessStateModel
{
    public Guid ProcessId { get; set; }

    public string ProcessName { get; set; } = string.Empty;

    public string ProcessKey { get; set; } = string.Empty;

    public string? AggregateId { get; set; }

    public string FlowType { get; set; } = "synchronous";

    public string Status { get; set; } = "started";

    public int CurrentStepOrdinal { get; set; }

    public string CurrentStepName { get; set; } = "created";

    public int Version { get; set; } = 1;

    public string IdempotencyKey { get; set; } = string.Empty;

    public string CorrelationId { get; set; } = string.Empty;

    public string? CausationId { get; set; }

    public DateTimeOffset IntentPersistedAt { get; set; }

    public DateTimeOffset? SyncDeadlineAt { get; set; }

    public string RecoveryPolicy { get; set; } = "resume_on_restart";

    public bool AwaitingClientDecision { get; set; }

    public DateTimeOffset? ClientDecisionDeadlineAt { get; set; }

    public int RetryCount { get; set; }

    public int MaxRetryCount { get; set; } = 20;

    public DateTimeOffset NextVisibleAt { get; set; }

    public string? LeaseOwner { get; set; }

    public DateTimeOffset? LeaseAcquiredAt { get; set; }

    public DateTimeOffset? LeaseExpiresAt { get; set; }

    public DateTimeOffset? HeartbeatAt { get; set; }

    public DateTimeOffset? InterruptedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public int? ResponseStatusCode { get; set; }

    public DateTimeOffset? ResponseCommittedAt { get; set; }

    public string? LastErrorCode { get; set; }

    public string? LastErrorMessage { get; set; }

    public string ProcessContext { get; set; } = "{}";

    public string InputPayload { get; set; } = "{}";

    public string ResultPayload { get; set; } = "{}";

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
