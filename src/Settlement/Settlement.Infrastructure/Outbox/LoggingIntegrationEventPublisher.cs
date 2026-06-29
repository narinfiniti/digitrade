using DigiTrade.Messaging.Contracts;
using Microsoft.Extensions.Logging;

namespace Settlement.Infrastructure.Outbox;

public sealed class LoggingIntegrationEventPublisher(ILogger<LoggingIntegrationEventPublisher> logger) : IIntegrationEventPublisher
{
    private static readonly Action<ILogger, string, Guid, string, Exception?> PublishSuccessLog =
        LoggerMessage.Define<string, Guid, string>(
            LogLevel.Information,
            new EventId(1, nameof(LoggingIntegrationEventPublisher)),
            "Published settlement integration event {EventName} ({EventId}) for aggregate {AggregateId}.");

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