namespace BffOrchestratorService.Domain.Options;

public sealed class ProcessRuntimeOutboxStoreOptions
{
    public const string SectionName = "ProcessRuntimeOutboxStore";

    public int MaxPublishAttempts { get; set; } = 10;

    public TimeSpan BaseRetryDelay { get; set; } = TimeSpan.FromSeconds(5);

    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromMinutes(5);

    public TimeSpan ProcessingLeaseTimeout { get; set; } = TimeSpan.FromMinutes(2);
}
