using System.Diagnostics;
using System.Diagnostics.Metrics;
using BffOrchestratorService.Domain;
using BffOrchestratorService.Domain.Abstractions;
using BffOrchestratorService.Domain.Observability;
using BffOrchestratorService.Infrastructure.Options;
using DigiTrade.Messaging.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BffOrchestratorService.Infrastructure.Services;

public sealed partial class ProcessRuntimeOutboxPublisherWorker(
    IServiceScopeFactory serviceScopeFactory,
    IIntegrationEventPublisher integrationEventPublisher,
    IOptions<ProcessRuntimeOutboxPublisherOptions> options,
    ILogger<ProcessRuntimeOutboxPublisherWorker> logger,
    TimeProvider timeProvider) : BackgroundService
{
    private static readonly Meter OutboxPublisherMeter = new(ProcessRuntimeOutboxMetrics.MeterName, ProcessRuntimeOutboxMetrics.MeterVersion);
    private static readonly Counter<long> ClaimedMessagesCounter = OutboxPublisherMeter.CreateCounter<long>("outbox_messages_claimed_total");
    private static readonly Counter<long> PublishedMessagesCounter = OutboxPublisherMeter.CreateCounter<long>("outbox_messages_published_total");
    private static readonly Counter<long> FailedMessagesCounter = OutboxPublisherMeter.CreateCounter<long>("outbox_messages_failed_total");
    private static readonly Counter<long> MarkPublishedErrorCounter = OutboxPublisherMeter.CreateCounter<long>("outbox_mark_published_errors_total");
    private static readonly Counter<long> MarkFailedErrorCounter = OutboxPublisherMeter.CreateCounter<long>("outbox_mark_failed_errors_total");
    private static readonly Counter<long> UnresolvedPublishedMessagesCounter = OutboxPublisherMeter.CreateCounter<long>("outbox_messages_unresolved_after_publish_total");
    private static readonly Counter<long> UnresolvedFailedMessagesCounter = OutboxPublisherMeter.CreateCounter<long>("outbox_messages_unresolved_after_fail_total");
    private static readonly Counter<long> PublishCyclesCounter = OutboxPublisherMeter.CreateCounter<long>("outbox_publish_cycles_total");
    private static readonly Counter<long> IdlePublishCyclesCounter = OutboxPublisherMeter.CreateCounter<long>("outbox_publish_idle_cycles_total");
    private static readonly Counter<long> PublishCancellationCounter = OutboxPublisherMeter.CreateCounter<long>("outbox_publish_cancellations_total");
    private static readonly Counter<long> DeferredClaimedMessagesCounter = OutboxPublisherMeter.CreateCounter<long>("outbox_claimed_messages_deferred_total");
    private static readonly Histogram<double> PublishCycleDurationHistogram = OutboxPublisherMeter.CreateHistogram<double>("outbox_publish_cycle_duration_ms", unit: "ms");
    private static readonly Histogram<long> ClaimedBatchSizeHistogram = OutboxPublisherMeter.CreateHistogram<long>("outbox_claimed_batch_size", unit: "messages");

    private readonly ProcessRuntimeOutboxPublisherOptions publisherOptions = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!publisherOptions.Enabled)
        {
            LogPublisherDisabled(logger);
            return;
        }

        await RunPublishCycleSafelyAsync(stoppingToken);

        using var timer = new PeriodicTimer(publisherOptions.PollInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunPublishCycleSafelyAsync(stoppingToken);
        }
    }

    private async Task RunPublishCycleSafelyAsync(CancellationToken cancellationToken)
    {
        var cycleStopwatch = Stopwatch.StartNew();

        try
        {
            await PublishOnceAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            PublishCancellationCounter.Add(1);
            // Host shutdown cancellation is expected; let ExecuteAsync exit gracefully.
        }
        catch (Exception exception)
        {
            LogPublishCycleFailed(logger, exception);
        }
        finally
        {
            cycleStopwatch.Stop();
            PublishCyclesCounter.Add(1);
            PublishCycleDurationHistogram.Record(cycleStopwatch.Elapsed.TotalMilliseconds);
        }
    }

    private async Task PublishOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var outboxStore = scope.ServiceProvider.GetRequiredService<IProcessRuntimeOutboxStore>();
        var pendingMessages = await outboxStore.GetPendingAsync(publisherOptions.BatchSize, cancellationToken);
        var claimedCount = pendingMessages.Count;
        var publishedCount = 0;
        var failedCount = 0;
        var markPublishedErrorCount = 0;
        var markFailedErrorCount = 0;

        if (claimedCount == 0)
        {
            IdlePublishCyclesCounter.Add(1);
            return;
        }

        ClaimedMessagesCounter.Add(claimedCount);
        ClaimedBatchSizeHistogram.Record(claimedCount);

        foreach (var pendingMessage in pendingMessages)
        {
            try
            {
                var envelope = ProcessRuntimeOutboxEnvelopeFactory.Create(pendingMessage);
                await integrationEventPublisher.PublishAsync(envelope, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                PublishCancellationCounter.Add(1);
                LogPublishCancelled(logger, pendingMessage.MessageId, pendingMessage.EventName);
                break;
            }
            catch (Exception publishException)
            {
                LogPublishFailed(logger, pendingMessage.MessageId, pendingMessage.EventName, publishException);

                try
                {
                    await outboxStore.MarkFailedAsync(
                        pendingMessage.MessageId,
                        publishException.Message,
                        timeProvider.GetUtcNow(),
                        cancellationToken);
                    failedCount += 1;
                    FailedMessagesCounter.Add(1);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    PublishCancellationCounter.Add(1);
                    LogPublishCancelled(logger, pendingMessage.MessageId, pendingMessage.EventName);
                    break;
                }
                catch (Exception markFailedException)
                {
                    markFailedErrorCount += 1;
                    MarkFailedErrorCounter.Add(1);
                    UnresolvedFailedMessagesCounter.Add(1);
                    LogMarkFailedPersistError(logger, pendingMessage.MessageId, pendingMessage.EventName, markFailedException);
                }

                continue;
            }

            try
            {
                await outboxStore.MarkPublishedAsync(pendingMessage.MessageId, timeProvider.GetUtcNow(), cancellationToken);
                publishedCount += 1;
                PublishedMessagesCounter.Add(1);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                PublishCancellationCounter.Add(1);
                LogPublishCancelled(logger, pendingMessage.MessageId, pendingMessage.EventName);
                break;
            }
            catch (Exception markPublishedException)
            {
                markPublishedErrorCount += 1;
                MarkPublishedErrorCounter.Add(1);
                UnresolvedPublishedMessagesCounter.Add(1);
                LogMarkPublishedPersistError(logger, pendingMessage.MessageId, pendingMessage.EventName, markPublishedException);
            }
        }

        if (publisherOptions.EmitCycleSummaryLogs)
        {
            var deferredClaimedCount = claimedCount - (publishedCount + failedCount + markPublishedErrorCount + markFailedErrorCount);
            if (deferredClaimedCount > 0)
            {
                DeferredClaimedMessagesCounter.Add(deferredClaimedCount);
            }

            LogPublishCycleSummary(
                logger,
                claimedCount,
                publishedCount,
                failedCount,
                markPublishedErrorCount,
                markFailedErrorCount,
                deferredClaimedCount);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Process runtime outbox publisher worker is disabled.")]
    private static partial void LogPublisherDisabled(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Process runtime outbox publish failed for message {MessageId} and event {EventName}.")]
    private static partial void LogPublishFailed(ILogger logger, Guid messageId, string eventName, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Process runtime outbox publish cycle failed and will be retried on next poll.")]
    private static partial void LogPublishCycleFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Process runtime outbox mark-failed persistence failed for message {MessageId} and event {EventName}.")]
    private static partial void LogMarkFailedPersistError(ILogger logger, Guid messageId, string eventName, Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "Process runtime outbox mark-published persistence failed for message {MessageId} and event {EventName}.")]
    private static partial void LogMarkPublishedPersistError(ILogger logger, Guid messageId, string eventName, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Process runtime outbox publish cancelled for message {MessageId} and event {EventName} during shutdown.")]
    private static partial void LogPublishCancelled(ILogger logger, Guid messageId, string eventName);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Process runtime outbox publish cycle summary. Claimed: {ClaimedCount}, Published: {PublishedCount}, Failed: {FailedCount}, MarkPublishedErrors: {MarkPublishedErrorCount}, MarkFailedErrors: {MarkFailedErrorCount}, DeferredClaimed: {DeferredClaimedCount}.")]
    private static partial void LogPublishCycleSummary(
        ILogger logger,
        int claimedCount,
        int publishedCount,
        int failedCount,
        int markPublishedErrorCount,
        int markFailedErrorCount,
        int deferredClaimedCount);
}