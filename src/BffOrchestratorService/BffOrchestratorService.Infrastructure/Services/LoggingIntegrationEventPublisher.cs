using DigiTrade.Messaging.Contracts;
using Microsoft.Extensions.Logging;

namespace BffOrchestratorService.Infrastructure.Services;

public sealed partial class LoggingIntegrationEventPublisher(ILogger<LoggingIntegrationEventPublisher> logger) : IIntegrationEventPublisher
{
    public Task PublishAsync(IntegrationEnvelope envelope, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        cancellationToken.ThrowIfCancellationRequested();

        LogPublishSuccess(
            logger,
            envelope.IntegrationEvent.EventName,
            envelope.IntegrationEvent.EventId,
            envelope.IntegrationEvent.EventVersion,
            envelope.IntegrationEvent.AggregateId,
            envelope.PartitionKey,
            envelope.CorrelationId,
            envelope.Headers.Count,
            envelope.IntegrationEvent.OccurredAtUtc,
            null);

        return Task.CompletedTask;
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Published process runtime integration event {EventName} ({EventId}, v{EventVersion}) for aggregate {AggregateId}, partition {PartitionKey}, correlation {CorrelationId}, headers {HeaderCount}, occurred {OccurredAtUtc}.")]
    private static partial void LogPublishSuccess(
        ILogger logger,
        string eventName,
        Guid eventId,
        int eventVersion,
        string aggregateId,
        string partitionKey,
        string? correlationId,
        int headerCount,
        DateTimeOffset occurredAtUtc,
        Exception? exception);
}