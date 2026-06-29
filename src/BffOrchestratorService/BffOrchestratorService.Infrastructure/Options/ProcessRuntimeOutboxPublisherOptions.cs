namespace BffOrchestratorService.Infrastructure.Options;

public sealed class ProcessRuntimeOutboxPublisherOptions
{
    public const string SectionName = "ProcessRuntimeOutboxPublisher";

    public bool Enabled { get; set; }

    public int BatchSize { get; set; } = 32;

    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);

    public bool EmitCycleSummaryLogs { get; set; } = true;
}