using System.Text.Json;
using DigiTrade.Messaging.Contracts;
using DigiTrade.Messaging.Persistence.Outbox;
using Risk.Application.Events;

namespace Risk.Infrastructure.Outbox;

internal static class RiskOutboxEnvelopeFactory
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static IntegrationEnvelope Create(OutboxMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var headers = string.IsNullOrWhiteSpace(message.HeadersJson)
            ? new Dictionary<string, string>(StringComparer.Ordinal)
            : JsonSerializer.Deserialize<Dictionary<string, string>>(message.HeadersJson, SerializerOptions)
                ?? new Dictionary<string, string>(StringComparer.Ordinal);

        headers["outbox-message-id"] = message.MessageId.ToString("D");

        return new IntegrationEnvelope(
            CreateIntegrationEvent(message),
            message.PartitionKey,
            null,
            headers);
    }

    private static IIntegrationEvent CreateIntegrationEvent(OutboxMessage message)
    {
        return message.EventName switch
        {
            MarginAccountOpenedIntegrationEvent.IntegrationEventName => Deserialize<MarginAccountOpenedIntegrationEvent>(message),
            MarginReservedIntegrationEvent.IntegrationEventName => Deserialize<MarginReservedIntegrationEvent>(message),
            MarginReleasedIntegrationEvent.IntegrationEventName => Deserialize<MarginReleasedIntegrationEvent>(message),
            _ => throw new InvalidOperationException($"Risk outbox does not support integration event '{message.EventName}'."),
        };
    }

    private static TIntegrationEvent Deserialize<TIntegrationEvent>(OutboxMessage message)
        where TIntegrationEvent : class, IIntegrationEvent
    {
        var integrationEvent = JsonSerializer.Deserialize<TIntegrationEvent>(message.Payload, SerializerOptions);
        return integrationEvent
            ?? throw new InvalidOperationException($"Risk outbox payload could not be deserialized for '{message.EventName}'.");
    }
}