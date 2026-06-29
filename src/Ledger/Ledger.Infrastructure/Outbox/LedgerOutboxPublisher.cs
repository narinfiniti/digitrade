using System.Text.Json;
using DigiTrade.Messaging.Contracts;
using DigiTrade.Messaging.Persistence.Outbox;
using Ledger.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Ledger.Infrastructure.Outbox;

public sealed class LedgerOutboxPublisher(
    IOutboxStore outboxStore,
    IIntegrationEventPublisher integrationEventPublisher,
    ILogger<LedgerOutboxPublisher> logger,
    TimeProvider timeProvider) : ILedgerOutboxPublisher
{
    private const int PublishBatchSize = 32;
    private static readonly Action<ILogger, Guid, string, Exception?> PublishFailureLog =
        LoggerMessage.Define<Guid, string>(
            LogLevel.Error,
            new EventId(1, nameof(LedgerOutboxPublisher)),
            "Ledger outbox publish failed for message {MessageId} and event {EventName}.");

    public async Task PublishPendingAsync(CancellationToken cancellationToken = default)
    {
        var pendingMessages = await outboxStore.GetPendingAsync(PublishBatchSize, cancellationToken);

        foreach (var pendingMessage in pendingMessages)
        {
            try
            {
                var envelope = LedgerOutboxEnvelopeFactory.Create(pendingMessage);
                await integrationEventPublisher.PublishAsync(envelope, cancellationToken);
                await outboxStore.MarkPublishedAsync(pendingMessage.MessageId, timeProvider.GetUtcNow(), cancellationToken);
            }
            catch (Exception exception)
            {
                PublishFailureLog(
                    logger,
                    pendingMessage.MessageId,
                    pendingMessage.EventName,
                    exception);

                await outboxStore.MarkFailedAsync(
                    pendingMessage.MessageId,
                    exception.Message,
                    timeProvider.GetUtcNow(),
                    cancellationToken);
            }
        }
    }
}

public sealed class LoggingIntegrationEventPublisher(ILogger<LoggingIntegrationEventPublisher> logger) : IIntegrationEventPublisher
{
    private static readonly Action<ILogger, string, Guid, string, Exception?> PublishSuccessLog =
        LoggerMessage.Define<string, Guid, string>(
            LogLevel.Information,
            new EventId(1, nameof(LoggingIntegrationEventPublisher)),
            "Published ledger integration event {EventName} ({EventId}) for aggregate {AggregateId}.");

    public Task PublishAsync(IntegrationEnvelope envelope, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        PublishSuccessLog(
            logger,
            envelope.IntegrationEvent.EventName,
            envelope.IntegrationEvent.EventId,
            envelope.IntegrationEvent.AggregateId,
            null);

        return Task.CompletedTask;
    }
}

internal static class LedgerOutboxEnvelopeFactory
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

    private static LedgerEntryPostedIntegrationEvent CreateIntegrationEvent(OutboxMessage message)
    {
        return message.EventName switch
        {
            LedgerEntryPostedIntegrationEvent.IntegrationEventName => Deserialize<LedgerEntryPostedIntegrationEvent>(message),
            _ => throw new InvalidOperationException($"Ledger outbox does not support integration event '{message.EventName}'."),
        };
    }

    private static TIntegrationEvent Deserialize<TIntegrationEvent>(OutboxMessage message)
        where TIntegrationEvent : class, IIntegrationEvent
    {
        var integrationEvent = JsonSerializer.Deserialize<TIntegrationEvent>(message.Payload, SerializerOptions);
        return integrationEvent
            ?? throw new InvalidOperationException($"Ledger outbox payload could not be deserialized for '{message.EventName}'.");
    }
}

internal sealed record LedgerEntryPostedIntegrationEvent(
    Guid EventId,
    string AggregateId,
    string SettlementId,
    string AccountId,
    string CurrencyCode,
    decimal TotalAmount,
    DateTimeOffset OccurredAtUtc) : IIntegrationEvent
{
    public const string IntegrationEventName = "ledger.entry.posted";

    public string EventName => IntegrationEventName;

    public int EventVersion => 1;
}