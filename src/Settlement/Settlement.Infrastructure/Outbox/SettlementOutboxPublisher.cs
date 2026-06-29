using DigiTrade.Messaging.Contracts;
using DigiTrade.Messaging.Persistence.Outbox;
using Microsoft.Extensions.Logging;
using Settlement.Application.Abstractions;

namespace Settlement.Infrastructure.Outbox;

public sealed class SettlementOutboxPublisher(
    IOutboxStore outboxStore,
    IIntegrationEventPublisher integrationEventPublisher,
    ILogger<SettlementOutboxPublisher> logger,
    TimeProvider timeProvider) : ISettlementOutboxPublisher
{
    private const int PublishBatchSize = 32;
    private static readonly Action<ILogger, Guid, string, Exception?> PublishFailureLog =
        LoggerMessage.Define<Guid, string>(
            LogLevel.Error,
            new EventId(1, nameof(SettlementOutboxPublisher)),
            "Settlement outbox publish failed for message {MessageId} and event {EventName}.");

    public async Task PublishPendingAsync(CancellationToken cancellationToken = default)
    {
        var pendingMessages = await outboxStore.GetPendingAsync(PublishBatchSize, cancellationToken);

        foreach (var pendingMessage in pendingMessages)
        {
            try
            {
                var envelope = SettlementOutboxEnvelopeFactory.Create(pendingMessage);
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