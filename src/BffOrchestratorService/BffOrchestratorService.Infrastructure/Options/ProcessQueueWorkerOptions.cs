namespace BffOrchestratorService.Infrastructure.Options;

public sealed class ProcessQueueWorkerOptions
{
    public const string SectionName = "ProcessQueueWorker";

    public bool Enabled { get; set; }

    public int BatchSize { get; set; } = 16;

    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);

    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromSeconds(30);

    public string LeaseOwner { get; set; } = "bff-orchestrator-supervision";
}