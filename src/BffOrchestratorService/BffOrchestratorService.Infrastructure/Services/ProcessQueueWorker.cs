using BffOrchestratorService.Domain;
using BffOrchestratorService.Domain.Abstractions;
using BffOrchestratorService.Domain.Models;
using BffOrchestratorService.Infrastructure.Abstractions;
using BffOrchestratorService.Infrastructure.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BffOrchestratorService.Infrastructure.Services;

public sealed partial class ProcessQueueWorker : BackgroundService
{
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromMinutes(5);
    private const int MaxBackoffExponent = 8;
    private const double JitterRatio = 0.20d;

    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ProcessQueueWorkerOptions workerOptions;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<ProcessQueueWorker> logger;
    private readonly string leaseOwner;

    public ProcessQueueWorker(
        IServiceScopeFactory serviceScopeFactory,
        IOptions<ProcessQueueWorkerOptions> options,
        TimeProvider timeProvider,
        ILogger<ProcessQueueWorker> logger)
    {
        ArgumentNullException.ThrowIfNull(serviceScopeFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        this.serviceScopeFactory = serviceScopeFactory;
        workerOptions = options.Value;
        this.timeProvider = timeProvider;
        this.logger = logger;
        leaseOwner = $"{workerOptions.LeaseOwner}:{Environment.MachineName}";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!workerOptions.Enabled)
        {
            LogWorkerDisabled(logger);
            return;
        }

        if (!HasHandlers())
        {
            LogNoHandlersRegistered(logger);
            return;
        }

        try
        {
            await PollOnceAsync(stoppingToken);

            using var timer = new PeriodicTimer(workerOptions.PollInterval);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await PollOnceAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            LogWorkerCancelledForShutdown(logger);
        }
    }

    private bool HasHandlers()
    {
        using var scope = serviceScopeFactory.CreateScope();
        return scope.ServiceProvider.GetServices<IProcessQueueWorkItemHandler>().Any();
    }

    private async Task PollOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var queueStore = scope.ServiceProvider.GetRequiredService<IProcessQueueStore>();
        var handlers = scope.ServiceProvider.GetServices<IProcessQueueWorkItemHandler>().ToArray();

        IReadOnlyCollection<ProcessQueueItemModel> leasedItems;

        try
        {
            leasedItems = await queueStore.LeaseReadyAsync(
                workerOptions.BatchSize,
                timeProvider.GetUtcNow(),
                leaseOwner,
                workerOptions.LeaseDuration,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            LogLeasePollingCancelledForShutdown(logger);
            return;
        }

        if (leasedItems.Count == 0)
        {
            return;
        }

        LogLeasedItems(logger, leasedItems.Count);

        foreach (var queueItem in leasedItems)
        {
            bool leaseRenewed;

            try
            {
                leaseRenewed = await queueStore.RenewLeaseAsync(
                    queueItem.Id,
                    timeProvider.GetUtcNow(),
                    leaseOwner,
                    workerOptions.LeaseDuration,
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                LogLeaseRenewalCancelledForShutdown(logger, queueItem.Id);
                break;
            }

            if (!leaseRenewed)
            {
                LogLeaseRenewalRejected(logger, queueItem.Id);
                continue;
            }

            var handler = handlers.FirstOrDefault(candidate => candidate.CanHandle(queueItem));

            if (handler is null)
            {
                LogMissingHandler(logger, queueItem.WorkType, queueItem.Id);
                bool deadLettered;

                try
                {
                    deadLettered = await queueStore.DeadLetterAsync(
                        queueItem.Id,
                        timeProvider.GetUtcNow(),
                        leaseOwner,
                        "missing_handler",
                        $"No process queue handler is registered for work type '{queueItem.WorkType}'.",
                        cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    LogDeadLetterCancelledForShutdown(logger, queueItem.Id);
                    break;
                }

                if (!deadLettered)
                {
                    LogDeadLetterRejected(logger, queueItem.Id);
                }

                continue;
            }

            try
            {
                await handler.HandleAsync(queueItem, leaseOwner, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                LogHandlerCancelledForShutdown(logger, queueItem.Id, queueItem.WorkType);
                break;
            }
            catch (Exception exception)
            {
                var observedAt = timeProvider.GetUtcNow();
                var reachedRetryLimit = queueItem.AttemptCount >= queueItem.MaxAttemptCount;

                if (reachedRetryLimit)
                {
                    LogHandlerFailedDeadLetter(
                        logger,
                        exception,
                        queueItem.Id,
                        queueItem.WorkType,
                        queueItem.AttemptCount,
                        queueItem.MaxAttemptCount);

                    bool deadLettered;

                    try
                    {
                        deadLettered = await queueStore.DeadLetterAsync(
                            queueItem.Id,
                            observedAt,
                            leaseOwner,
                            "handler_failed",
                            exception.Message,
                            cancellationToken);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        LogDeadLetterCancelledForShutdown(logger, queueItem.Id);
                        break;
                    }

                    if (!deadLettered)
                    {
                        LogDeadLetterRejected(logger, queueItem.Id);
                    }

                    continue;
                }

                var nextVisibleAt = ComputeNextVisibleAt(queueItem, observedAt);
                LogHandlerFailedRetry(
                    logger,
                    exception,
                    queueItem.Id,
                    queueItem.WorkType,
                    queueItem.AttemptCount,
                    queueItem.MaxAttemptCount,
                    nextVisibleAt);

                bool requeued;

                try
                {
                    requeued = await queueStore.RequeueAsync(
                        queueItem.Id,
                        observedAt,
                        nextVisibleAt,
                        leaseOwner,
                        "handler_failed",
                        exception.Message,
                        cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    LogRequeueCancelledForShutdown(logger, queueItem.Id);
                    break;
                }

                if (!requeued)
                {
                    LogRequeueRejected(logger, queueItem.Id);
                }

                continue;
            }

            bool completed;

            try
            {
                completed = await queueStore.CompleteAsync(
                    queueItem.Id,
                    timeProvider.GetUtcNow(),
                    leaseOwner,
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                LogCompletionCancelledForShutdown(logger, queueItem.Id);
                break;
            }

            if (!completed)
            {
                LogCompletionRejected(logger, queueItem.Id);
            }
        }
    }

    private DateTimeOffset ComputeNextVisibleAt(ProcessQueueItemModel queueItem, DateTimeOffset observedAt)
    {
        var attemptNumber = Math.Max(queueItem.AttemptCount, 1);
        var exponent = Math.Min(attemptNumber - 1, MaxBackoffExponent);
        var baseDelayMilliseconds = workerOptions.PollInterval.TotalMilliseconds;

        var exponentialDelayMilliseconds = baseDelayMilliseconds * Math.Pow(2, exponent);
        var boundedDelayMilliseconds = Math.Min(exponentialDelayMilliseconds, MaxRetryDelay.TotalMilliseconds);

        var jitterWindowMilliseconds = boundedDelayMilliseconds * JitterRatio;
        var jitterMilliseconds = (Random.Shared.NextDouble() * 2d - 1d) * jitterWindowMilliseconds;
        var effectiveDelayMilliseconds = Math.Max(baseDelayMilliseconds, boundedDelayMilliseconds + jitterMilliseconds);

        return observedAt.AddMilliseconds(effectiveDelayMilliseconds);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Process queue worker shell is disabled.")]
    private static partial void LogWorkerDisabled(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Process queue worker shell cancellation requested during host shutdown.")]
    private static partial void LogWorkerCancelledForShutdown(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Process queue worker shell is enabled but no process queue handlers are registered.")]
    private static partial void LogNoHandlersRegistered(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Process queue worker shell leased {QueueItemCount} supervision item(s).")]
    private static partial void LogLeasedItems(ILogger logger, int queueItemCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No process queue handler is registered for work type {WorkType} on queue item {QueueItemId}.")]
    private static partial void LogMissingHandler(ILogger logger, string workType, long queueItemId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Process queue completion was rejected for queue item {QueueItemId} after handler execution.")]
    private static partial void LogCompletionRejected(ILogger logger, long queueItemId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Process queue completion was cancelled during host shutdown for queue item {QueueItemId}.")]
    private static partial void LogCompletionCancelledForShutdown(ILogger logger, long queueItemId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Process queue dead-letter transition was rejected for queue item {QueueItemId}.")]
    private static partial void LogDeadLetterRejected(ILogger logger, long queueItemId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Process queue dead-letter transition was cancelled during host shutdown for queue item {QueueItemId}.")]
    private static partial void LogDeadLetterCancelledForShutdown(ILogger logger, long queueItemId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Process queue handler failed and item {QueueItemId} ({WorkType}) will be retried. Attempt {AttemptCount} of {MaxAttemptCount}. Next visibility: {NextVisibleAt}.")]
    private static partial void LogHandlerFailedRetry(
        ILogger logger,
        Exception exception,
        long queueItemId,
        string workType,
        int attemptCount,
        int maxAttemptCount,
        DateTimeOffset nextVisibleAt);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Process queue handler failed and item {QueueItemId} ({WorkType}) reached retry limit. Attempt {AttemptCount} of {MaxAttemptCount}. Dead-lettering.")]
    private static partial void LogHandlerFailedDeadLetter(
        ILogger logger,
        Exception exception,
        long queueItemId,
        string workType,
        int attemptCount,
        int maxAttemptCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Process queue requeue transition was rejected for queue item {QueueItemId}.")]
    private static partial void LogRequeueRejected(ILogger logger, long queueItemId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Process queue requeue transition was cancelled during host shutdown for queue item {QueueItemId}.")]
    private static partial void LogRequeueCancelledForShutdown(ILogger logger, long queueItemId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Process queue lease renewal was rejected for queue item {QueueItemId} before handler dispatch.")]
    private static partial void LogLeaseRenewalRejected(ILogger logger, long queueItemId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Process queue lease polling was cancelled during host shutdown.")]
    private static partial void LogLeasePollingCancelledForShutdown(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Process queue lease renewal was cancelled during host shutdown for queue item {QueueItemId}.")]
    private static partial void LogLeaseRenewalCancelledForShutdown(ILogger logger, long queueItemId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Process queue handler execution was cancelled during host shutdown for queue item {QueueItemId} ({WorkType}).")]
    private static partial void LogHandlerCancelledForShutdown(ILogger logger, long queueItemId, string workType);
}