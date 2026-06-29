namespace BffOrchestratorService.Infrastructure.Options;

public sealed class KafkaIntegrationPublisherOptions
{
    public const string SectionName = "KafkaIntegrationPublisher";

    public string BootstrapServers { get; set; } = "localhost:9092";

    public string TopicName { get; set; } = "digitrade.process-runtime-events";

    public string ClientId { get; set; } = "bff-orchestrator-process-runtime-outbox";

    public bool EnableIdempotence { get; set; } = true;
}