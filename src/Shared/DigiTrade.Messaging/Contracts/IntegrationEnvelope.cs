namespace DigiTrade.Messaging.Contracts;

public sealed record IntegrationEnvelope(
    IIntegrationEvent IntegrationEvent,
    string PartitionKey,
    string? CorrelationId,
    IReadOnlyDictionary<string, string> Headers);