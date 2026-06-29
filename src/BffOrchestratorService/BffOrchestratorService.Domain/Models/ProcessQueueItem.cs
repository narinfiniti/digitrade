namespace BffOrchestratorService.Domain.Models;

public sealed class ProcessQueueItemModel
{
    public long Id { get; set; }

    public Guid ProcessId { get; set; }

    public string ProcessKey { get; set; } = string.Empty;

    public string FlowType { get; set; } = "synchronous";

    public string WorkType { get; set; } = "start";

    public string Status { get; set; } = "ready";

    public short Priority { get; set; } = 100;

    public DateTimeOffset VisibleAt { get; set; }

    public long SequenceNo { get; set; }

    public int AttemptCount { get; set; }

    public int MaxAttemptCount { get; set; } = 20;

    public string? LeaseOwner { get; set; }

    public DateTimeOffset? LeaseAcquiredAt { get; set; }

    public DateTimeOffset? LeaseExpiresAt { get; set; }

    public string DedupeKey { get; set; } = string.Empty;

    public string Payload { get; set; } = "{}";

    public string? LastErrorCode { get; set; }

    public string? LastErrorMessage { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
