using DigiTrade.Messaging.Contracts;
using DigiTrade.Messaging.Persistence.Outbox;
using Microsoft.Extensions.Logging;
using Trade.Application.Abstractions;

namespace Trade.Infrastructure.Outbox;

public sealed class TradeOutboxPublisher(
    IOutboxStore outboxStore,
    IIntegrationEventPublisher integrationEventPublisher,
    ILogger<TradeOutboxPublisher> logger,
    TimeProvider timeProvider) : ITradeOutboxPublisher
{
    private const int PublishBatchSize = 32;
    private static readonly Action<ILogger, Guid, string, Exception?> PublishFailureLog =
        LoggerMessage.Define<Guid, string>(
            LogLevel.Error,
            new EventId(1, nameof(TradeOutboxPublisher)),
            "Trade outbox publish failed for message {MessageId} and event {EventName}.");

    public async Task PublishPendingAsync(CancellationToken cancellationToken = default)
    {
        var pendingMessages = await outboxStore.GetPendingAsync(PublishBatchSize, cancellationToken);

        foreach (var pendingMessage in pendingMessages)
        {
            try
            {
                var envelope = TradeOutboxEnvelopeFactory.Create(pendingMessage);
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