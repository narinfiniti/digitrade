namespace DigiTrade.Messaging.Configuration;

public sealed class KafkaIntegrationPublisherOptions
{
    public const string SectionName = "KafkaIntegrationPublisher";

    public string BootstrapServers { get; set; } = "localhost:9092";

    public string TopicName { get; set; } = "digitrade.domain-events";

    public string ClientId { get; set; } = "digitrade-domain-events";

    public bool EnableIdempotence { get; set; } = true;
}