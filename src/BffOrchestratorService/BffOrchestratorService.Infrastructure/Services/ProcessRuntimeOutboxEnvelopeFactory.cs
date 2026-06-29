using System.Globalization;
using System.Text.Json;
using DigiTrade.Messaging.Contracts;
using DigiTrade.Messaging.Persistence.Outbox;

namespace BffOrchestratorService.Infrastructure.Services;

internal static class ProcessRuntimeOutboxEnvelopeFactory
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static IntegrationEnvelope Create(OutboxMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var headers = ParseHeaders(message.HeadersJson);
        var partitionKey = string.IsNullOrWhiteSpace(message.PartitionKey)
            ? message.AggregateId
            : message.PartitionKey;

        headers["event-id"] = message.EventId.ToString("D");
        headers["outbox-message-id"] = message.MessageId.ToString("D");
        headers["event-name"] = message.EventName;
        headers["event-version"] = message.EventVersion.ToString(CultureInfo.InvariantCulture);
        headers["aggregate-id"] = message.AggregateId;
        headers["partition-key"] = partitionKey;
        headers["occurred-at-utc"] = message.OccurredAtUtc.ToString("O", CultureInfo.InvariantCulture);
        headers["outbox-status"] = message.Status.ToString();
        headers["outbox-attempt-count"] = message.AttemptCount.ToString(CultureInfo.InvariantCulture);

        if (message.TransactionId is Guid transactionId)
        {
            headers["transaction-id"] = transactionId.ToString("D");
        }

        var correlationId = headers.TryGetValue("correlation-id", out var value)
            && !string.IsNullOrWhiteSpace(value)
                ? value
                : message.TransactionId?.ToString("D");

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            headers["correlation-id"] = correlationId;
        }

        return new IntegrationEnvelope(
            new ProcessRuntimeOutboxIntegrationEvent(
                message.EventId,
                message.EventName,
                message.EventVersion,
                message.AggregateId,
                message.OccurredAtUtc,
                message.Payload),
            partitionKey,
            correlationId,
            headers);
    }

    private static Dictionary<string, string> ParseHeaders(string? headersJson)
    {
        if (string.IsNullOrWhiteSpace(headersJson))
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(headersJson, SerializerOptions)
                ?? new Dictionary<string, string>(StringComparer.Ordinal);
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }
        catch (NotSupportedException)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }
    }

    private sealed record ProcessRuntimeOutboxIntegrationEvent(
        Guid EventId,
        string EventName,
        int EventVersion,
        string AggregateId,
        DateTimeOffset OccurredAtUtc,
        string Payload) : IIntegrationEvent;
}