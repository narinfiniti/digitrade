using System.Text.Json;
using DigiTrade.Messaging.Contracts;
using DigiTrade.Messaging.Persistence.Outbox;
using Trade.Application.Events;

namespace Trade.Infrastructure.Outbox;

internal static class TradeOutboxEnvelopeFactory
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
            TradeOpenedIntegrationEvent.IntegrationEventName => Deserialize<TradeOpenedIntegrationEvent>(message),
            TradeClosedIntegrationEvent.IntegrationEventName => Deserialize<TradeClosedIntegrationEvent>(message),
            _ => throw new InvalidOperationException($"Trade outbox does not support integration event '{message.EventName}'."),
        };
    }

    private static TIntegrationEvent Deserialize<TIntegrationEvent>(OutboxMessage message)
        where TIntegrationEvent : class, IIntegrationEvent
    {
        var integrationEvent = JsonSerializer.Deserialize<TIntegrationEvent>(message.Payload, SerializerOptions);
        return integrationEvent
            ?? throw new InvalidOperationException($"Trade outbox payload could not be deserialized for '{message.EventName}'.");
    }
}