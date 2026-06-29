using System.Reflection;
using BffOrchestratorService.Domain;
using BffOrchestratorService.Domain.Abstractions;
using BffOrchestratorService.Infrastructure.Options;
using BffOrchestratorService.Infrastructure.Services;
using DigiTrade.Messaging.Contracts;
using DigiTrade.Messaging.Persistence.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BffOrchestratorService.Infrastructure.Tests;

public sealed class ProcessRuntimeOutboxPublisherWorkerTests
{
    [Fact]
    public async Task ExecuteAsyncWhenPublisherDisabledDoesNotPollOrPublish()
    {
        var outboxStore = new InMemoryOutboxStore();
        var publisher = new RecordingIntegrationEventPublisher();
        var worker = CreateWorker(outboxStore, publisher, enabled: false);

        await InvokeExecuteAsync(worker, CancellationToken.None);

        Assert.Equal(0, outboxStore.GetPendingCalls);
        Assert.Equal(0, outboxStore.MarkPublishedCalls);
        Assert.Equal(0, outboxStore.MarkFailedCalls);
        Assert.Equal(0, publisher.PublishCalls);
    }

    [Fact]
    public async Task ExecuteAsyncWhenEnabledAndTokenCancelledRunsInitialCycleThenPropagatesCancellation()
    {
        var outboxStore = new InMemoryOutboxStore
        {
            PendingMessages = [],
        };
        var publisher = new RecordingIntegrationEventPublisher();
        var worker = CreateWorker(outboxStore, publisher, enabled: true);

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(() => InvokeExecuteAsync(worker, cancellationTokenSource.Token));

        Assert.Equal(1, outboxStore.GetPendingCalls);
        Assert.Equal(0, outboxStore.MarkPublishedCalls);
        Assert.Equal(0, outboxStore.MarkFailedCalls);
        Assert.Equal(0, publisher.PublishCalls);
    }

    [Fact]
    public async Task ExecuteAsyncWhenPendingMessagePublishesMarksPublished()
    {
        var message = CreatePendingMessage();
        var outboxStore = new InMemoryOutboxStore
        {
            PendingMessages = [message],
        };
        var publisher = new RecordingIntegrationEventPublisher();
        var worker = CreateWorker(outboxStore, publisher, enabled: true);

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(() => InvokeExecuteAsync(worker, cancellationTokenSource.Token));

        Assert.Equal(1, outboxStore.GetPendingCalls);
        Assert.Equal(1, publisher.PublishCalls);
        Assert.Equal(1, outboxStore.MarkPublishedCalls);
        Assert.Equal(0, outboxStore.MarkFailedCalls);
    }

    [Fact]
    public async Task ExecuteAsyncWhenPublishFailsMarksFailed()
    {
        var message = CreatePendingMessage();
        var outboxStore = new InMemoryOutboxStore
        {
            PendingMessages = [message],
        };
        var publisher = new ThrowingIntegrationEventPublisher();
        var worker = CreateWorker(outboxStore, publisher, enabled: true);

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(() => InvokeExecuteAsync(worker, cancellationTokenSource.Token));

        Assert.Equal(1, outboxStore.GetPendingCalls);
        Assert.Equal(1, publisher.PublishCalls);
        Assert.Equal(0, outboxStore.MarkPublishedCalls);
        Assert.Equal(1, outboxStore.MarkFailedCalls);
    }

    [Fact]
    public async Task ExecuteAsyncWhenMarkPublishedFailsDoesNotMarkFailed()
    {
        var message = CreatePendingMessage();
        var outboxStore = new InMemoryOutboxStore
        {
            PendingMessages = [message],
            ThrowOnMarkPublished = true,
        };
        var publisher = new RecordingIntegrationEventPublisher();
        var worker = CreateWorker(outboxStore, publisher, enabled: true);

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(() => InvokeExecuteAsync(worker, cancellationTokenSource.Token));

        Assert.Equal(1, outboxStore.GetPendingCalls);
        Assert.Equal(1, publisher.PublishCalls);
        Assert.Equal(1, outboxStore.MarkPublishedCalls);
        Assert.Equal(0, outboxStore.MarkFailedCalls);
    }

    [Fact]
    public async Task ExecuteAsyncWhenMarkFailedFailsDoesNotMarkPublished()
    {
        var message = CreatePendingMessage();
        var outboxStore = new InMemoryOutboxStore
        {
            PendingMessages = [message],
            ThrowOnMarkFailed = true,
        };
        var publisher = new ThrowingIntegrationEventPublisher();
        var worker = CreateWorker(outboxStore, publisher, enabled: true);

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(() => InvokeExecuteAsync(worker, cancellationTokenSource.Token));

        Assert.Equal(1, outboxStore.GetPendingCalls);
        Assert.Equal(1, publisher.PublishCalls);
        Assert.Equal(0, outboxStore.MarkPublishedCalls);
        Assert.Equal(1, outboxStore.MarkFailedCalls);
    }

    [Fact]
    public async Task PublishOnceAsyncWhenPublishCancelledBreaksAndSkipsRemainingMessages()
    {
        var first = CreatePendingMessage();
        var second = CreatePendingMessage();
        var outboxStore = new InMemoryOutboxStore
        {
            PendingMessages = [first, second],
        };
        var publisher = new CancellationThrowingIntegrationEventPublisher();
        var worker = CreateWorker(outboxStore, publisher, enabled: true);

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await InvokePublishOnceAsync(worker, cancellationTokenSource.Token);

        Assert.Equal(1, outboxStore.GetPendingCalls);
        Assert.Equal(1, publisher.PublishCalls);
        Assert.Equal(0, outboxStore.MarkPublishedCalls);
        Assert.Equal(0, outboxStore.MarkFailedCalls);
    }

    [Fact]
    public async Task RunPublishCycleSafelyAsyncWhenStoreThrowsDoesNotEscapeException()
    {
        var outboxStore = new InMemoryOutboxStore
        {
            ThrowOnGetPending = true,
        };
        var publisher = new RecordingIntegrationEventPublisher();
        var worker = CreateWorker(outboxStore, publisher, enabled: true);

        await InvokeRunPublishCycleSafelyAsync(worker, CancellationToken.None);

        Assert.Equal(1, outboxStore.GetPendingCalls);
        Assert.Equal(0, publisher.PublishCalls);
        Assert.Equal(0, outboxStore.MarkPublishedCalls);
        Assert.Equal(0, outboxStore.MarkFailedCalls);
    }

    [Fact]
    public async Task RunPublishCycleSafelyAsyncWhenStoreThrowsCancellationDoesNotEscapeException()
    {
        var outboxStore = new InMemoryOutboxStore
        {
            ThrowOperationCanceledOnGetPending = true,
        };
        var publisher = new RecordingIntegrationEventPublisher();
        var worker = CreateWorker(outboxStore, publisher, enabled: true);

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await InvokeRunPublishCycleSafelyAsync(worker, cancellationTokenSource.Token);

        Assert.Equal(1, outboxStore.GetPendingCalls);
        Assert.Equal(0, publisher.PublishCalls);
        Assert.Equal(0, outboxStore.MarkPublishedCalls);
        Assert.Equal(0, outboxStore.MarkFailedCalls);
    }

    [Fact]
    public async Task PublishOnceAsyncWhenPublisherThrowsOperationCanceledWithoutShutdownMarksFailed()
    {
        var message = CreatePendingMessage();
        var outboxStore = new InMemoryOutboxStore
        {
            PendingMessages = [message],
        };
        var publisher = new OperationCanceledIntegrationEventPublisher();
        var worker = CreateWorker(outboxStore, publisher, enabled: true);

        await InvokePublishOnceAsync(worker, CancellationToken.None);

        Assert.Equal(1, outboxStore.GetPendingCalls);
        Assert.Equal(1, publisher.PublishCalls);
        Assert.Equal(0, outboxStore.MarkPublishedCalls);
        Assert.Equal(1, outboxStore.MarkFailedCalls);
    }

    [Fact]
    public async Task PublishOnceAsyncWhenMarkPublishedThrowsCancellationBreaksAndSkipsRemainingMessages()
    {
        var first = CreatePendingMessage();
        var second = CreatePendingMessage();
        var outboxStore = new InMemoryOutboxStore
        {
            PendingMessages = [first, second],
            ThrowOperationCanceledOnMarkPublished = true,
        };
        var publisher = new RecordingIntegrationEventPublisher();
        var worker = CreateWorker(outboxStore, publisher, enabled: true);

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await InvokePublishOnceAsync(worker, cancellationTokenSource.Token);

        Assert.Equal(1, outboxStore.GetPendingCalls);
        Assert.Equal(1, publisher.PublishCalls);
        Assert.Equal(1, outboxStore.MarkPublishedCalls);
        Assert.Equal(0, outboxStore.MarkFailedCalls);
    }

    [Fact]
    public async Task PublishOnceAsyncWhenMarkFailedThrowsCancellationBreaksAndSkipsRemainingMessages()
    {
        var first = CreatePendingMessage();
        var second = CreatePendingMessage();
        var outboxStore = new InMemoryOutboxStore
        {
            PendingMessages = [first, second],
            ThrowOperationCanceledOnMarkFailed = true,
        };
        var publisher = new ThrowingIntegrationEventPublisher();
        var worker = CreateWorker(outboxStore, publisher, enabled: true);

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await InvokePublishOnceAsync(worker, cancellationTokenSource.Token);

        Assert.Equal(1, outboxStore.GetPendingCalls);
        Assert.Equal(1, publisher.PublishCalls);
        Assert.Equal(0, outboxStore.MarkPublishedCalls);
        Assert.Equal(1, outboxStore.MarkFailedCalls);
    }

    [Fact]
    public async Task PublishOnceAsyncWhenFirstPublishFailsContinuesToNextMessage()
    {
        var first = CreatePendingMessage();
        var second = CreatePendingMessage();
        var outboxStore = new InMemoryOutboxStore
        {
            PendingMessages = [first, second],
        };
        var publisher = new SequenceIntegrationEventPublisher
        {
            ThrowOnFirstPublish = true,
        };
        var worker = CreateWorker(outboxStore, publisher, enabled: true);

        await InvokePublishOnceAsync(worker, CancellationToken.None);

        Assert.Equal(1, outboxStore.GetPendingCalls);
        Assert.Equal(2, publisher.PublishCalls);
        Assert.Equal(1, outboxStore.MarkPublishedCalls);
        Assert.Equal(1, outboxStore.MarkFailedCalls);
    }

    [Fact]
    public async Task PublishOnceAsyncWhenFirstMarkPublishedFailsContinuesToNextMessage()
    {
        var first = CreatePendingMessage();
        var second = CreatePendingMessage();
        var outboxStore = new InMemoryOutboxStore
        {
            PendingMessages = [first, second],
            ThrowOnFirstMarkPublishedCall = true,
        };
        var publisher = new RecordingIntegrationEventPublisher();
        var worker = CreateWorker(outboxStore, publisher, enabled: true);

        await InvokePublishOnceAsync(worker, CancellationToken.None);

        Assert.Equal(1, outboxStore.GetPendingCalls);
        Assert.Equal(2, publisher.PublishCalls);
        Assert.Equal(2, outboxStore.MarkPublishedCalls);
        Assert.Equal(0, outboxStore.MarkFailedCalls);
    }

    [Fact]
    public async Task PublishOnceAsyncWhenFirstMarkFailedFailsContinuesToNextMessage()
    {
        var first = CreatePendingMessage();
        var second = CreatePendingMessage();
        var outboxStore = new InMemoryOutboxStore
        {
            PendingMessages = [first, second],
            ThrowOnFirstMarkFailedCall = true,
        };
        var publisher = new SequenceIntegrationEventPublisher
        {
            ThrowOnFirstPublish = true,
        };
        var worker = CreateWorker(outboxStore, publisher, enabled: true);

        await InvokePublishOnceAsync(worker, CancellationToken.None);

        Assert.Equal(1, outboxStore.GetPendingCalls);
        Assert.Equal(2, publisher.PublishCalls);
        Assert.Equal(1, outboxStore.MarkPublishedCalls);
        Assert.Equal(1, outboxStore.MarkFailedCalls);
    }

    [Fact]
    public async Task PublishOnceAsyncWhenPartitionKeyMissingUsesAggregateIdAndTransactionIdCorrelation()
    {
        var transactionId = Guid.NewGuid();
        var message = CreatePendingMessage(
            aggregateId: "aggregate-123",
            partitionKey: " ",
            headersJson: null,
            transactionId: transactionId);
        var outboxStore = new InMemoryOutboxStore
        {
            PendingMessages = [message],
        };
        var publisher = new RecordingIntegrationEventPublisher();
        var worker = CreateWorker(outboxStore, publisher, enabled: true);

        await InvokePublishOnceAsync(worker, CancellationToken.None);

        Assert.Equal(1, publisher.PublishCalls);
        var envelope = Assert.Single(publisher.PublishedEnvelopes);
        Assert.Equal("aggregate-123", envelope.PartitionKey);
        Assert.Equal(transactionId.ToString("D"), envelope.CorrelationId);
        Assert.Equal(transactionId.ToString("D"), envelope.Headers["correlation-id"]);
        Assert.Equal("aggregate-123", envelope.Headers["partition-key"]);
        Assert.Equal(message.MessageId.ToString("D"), envelope.Headers["outbox-message-id"]);
    }

    [Fact]
    public async Task PublishOnceAsyncWhenCorrelationHeaderPresentPreservesCorrelationId()
    {
        var message = CreatePendingMessage(
            aggregateId: "aggregate-1",
            partitionKey: "partition-1",
            headersJson: "{\"correlation-id\":\"corr-123\"}",
            transactionId: Guid.NewGuid());
        var outboxStore = new InMemoryOutboxStore
        {
            PendingMessages = [message],
        };
        var publisher = new RecordingIntegrationEventPublisher();
        var worker = CreateWorker(outboxStore, publisher, enabled: true);

        await InvokePublishOnceAsync(worker, CancellationToken.None);

        var envelope = Assert.Single(publisher.PublishedEnvelopes);
        Assert.Equal("partition-1", envelope.PartitionKey);
        Assert.Equal("corr-123", envelope.CorrelationId);
        Assert.Equal("corr-123", envelope.Headers["correlation-id"]);
        Assert.Equal("partition-1", envelope.Headers["partition-key"]);
    }

    [Fact]
    public async Task PublishOnceAsyncWhenHeadersJsonMalformedPublishesWithGeneratedHeaders()
    {
        var message = CreatePendingMessage(
            aggregateId: "aggregate-7",
            partitionKey: "partition-7",
            headersJson: "{not-json",
            transactionId: null);
        var outboxStore = new InMemoryOutboxStore
        {
            PendingMessages = [message],
        };
        var publisher = new RecordingIntegrationEventPublisher();
        var worker = CreateWorker(outboxStore, publisher, enabled: true);

        await InvokePublishOnceAsync(worker, CancellationToken.None);

        var envelope = Assert.Single(publisher.PublishedEnvelopes);
        Assert.Null(envelope.CorrelationId);
        Assert.False(envelope.Headers.ContainsKey("correlation-id"));
        Assert.Equal("partition-7", envelope.PartitionKey);
        Assert.Equal("partition-7", envelope.Headers["partition-key"]);
        Assert.Equal(message.EventId.ToString("D"), envelope.Headers["event-id"]);
    }

    private static OutboxMessage CreatePendingMessage(
        string aggregateId = "process-1",
        string partitionKey = "process-1",
        string? headersJson = null,
        Guid? transactionId = null)
    {
        var now = TimeProvider.System.GetUtcNow();

        return new OutboxMessage(
            MessageId: Guid.NewGuid(),
            EventId: Guid.NewGuid(),
            EventName: "process.runtime.event",
            AggregateId: aggregateId,
            PartitionKey: partitionKey,
            EventVersion: 1,
            OccurredAtUtc: now,
            Payload: "{}",
            HeadersJson: headersJson,
            TransactionId: transactionId,
            Status: OutboxMessageStatus.Pending,
            AttemptCount: 0,
            LastAttemptAtUtc: null,
            PublishedAtUtc: null,
            FailureReason: null);
    }

    private static ProcessRuntimeOutboxPublisherWorker CreateWorker(
        IProcessRuntimeOutboxStore outboxStore,
        IIntegrationEventPublisher publisher,
        bool enabled)
    {
        var options = Microsoft.Extensions.Options.Options.Create(new ProcessRuntimeOutboxPublisherOptions
        {
            Enabled = enabled,
            BatchSize = 32,
            PollInterval = TimeSpan.FromMilliseconds(25),
            EmitCycleSummaryLogs = false,
        });

        return new ProcessRuntimeOutboxPublisherWorker(
            new StaticOutboxScopeFactory(outboxStore),
            publisher,
            options,
            NullLogger<ProcessRuntimeOutboxPublisherWorker>.Instance,
            TimeProvider.System);
    }

    private static async Task InvokeExecuteAsync(ProcessRuntimeOutboxPublisherWorker worker, CancellationToken cancellationToken)
    {
        var executeAsyncMethod = typeof(ProcessRuntimeOutboxPublisherWorker).GetMethod("ExecuteAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not find ExecuteAsync method.");

        var task = executeAsyncMethod.Invoke(worker, new object[] { cancellationToken }) as Task
            ?? throw new InvalidOperationException("ExecuteAsync invocation did not return a Task.");

        await task;
    }

    private static async Task InvokePublishOnceAsync(ProcessRuntimeOutboxPublisherWorker worker, CancellationToken cancellationToken)
    {
        var publishOnceAsyncMethod = typeof(ProcessRuntimeOutboxPublisherWorker).GetMethod("PublishOnceAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not find PublishOnceAsync method.");

        var task = publishOnceAsyncMethod.Invoke(worker, new object[] { cancellationToken }) as Task
            ?? throw new InvalidOperationException("PublishOnceAsync invocation did not return a Task.");

        await task;
    }

    private static async Task InvokeRunPublishCycleSafelyAsync(ProcessRuntimeOutboxPublisherWorker worker, CancellationToken cancellationToken)
    {
        var runPublishCycleSafelyAsyncMethod = typeof(ProcessRuntimeOutboxPublisherWorker).GetMethod("RunPublishCycleSafelyAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not find RunPublishCycleSafelyAsync method.");

        var task = runPublishCycleSafelyAsyncMethod.Invoke(worker, new object[] { cancellationToken }) as Task
            ?? throw new InvalidOperationException("RunPublishCycleSafelyAsync invocation did not return a Task.");

        await task;
    }

    private sealed class InMemoryOutboxStore : IProcessRuntimeOutboxStore
    {
        public int GetPendingCalls { get; private set; }

        public int MarkPublishedCalls { get; private set; }

        public int MarkFailedCalls { get; private set; }

        public bool ThrowOnMarkPublished { get; set; }

        public bool ThrowOnMarkFailed { get; set; }

        public bool ThrowOnFirstMarkPublishedCall { get; set; }

        public bool ThrowOnFirstMarkFailedCall { get; set; }

        public bool ThrowOperationCanceledOnMarkPublished { get; set; }

        public bool ThrowOperationCanceledOnMarkFailed { get; set; }

        public bool ThrowOnGetPending { get; set; }

        public bool ThrowOperationCanceledOnGetPending { get; set; }

        public IReadOnlyCollection<OutboxMessage> PendingMessages { get; set; } = [];

        public Task EnqueueAsync(OutboxMessage message, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyCollection<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken = default)
        {
            GetPendingCalls += 1;

            if (ThrowOnGetPending)
            {
                throw new InvalidOperationException("simulated get pending failure");
            }

            if (ThrowOperationCanceledOnGetPending)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            return Task.FromResult(PendingMessages);
        }

        public Task MarkPublishedAsync(Guid messageId, DateTimeOffset publishedAtUtc, CancellationToken cancellationToken = default)
        {
            MarkPublishedCalls += 1;

            if (ThrowOperationCanceledOnMarkPublished)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            if (ThrowOnFirstMarkPublishedCall && MarkPublishedCalls == 1)
            {
                throw new InvalidOperationException("simulated first mark published failure");
            }

            if (ThrowOnMarkPublished)
            {
                throw new InvalidOperationException("simulated mark published failure");
            }

            return Task.CompletedTask;
        }

        public Task MarkFailedAsync(Guid messageId, string failureReason, DateTimeOffset failedAtUtc, CancellationToken cancellationToken = default)
        {
            MarkFailedCalls += 1;

            if (ThrowOperationCanceledOnMarkFailed)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            if (ThrowOnFirstMarkFailedCall && MarkFailedCalls == 1)
            {
                throw new InvalidOperationException("simulated first mark failed failure");
            }

            if (ThrowOnMarkFailed)
            {
                throw new InvalidOperationException("simulated mark failed failure");
            }

            return Task.CompletedTask;
        }
    }

    private sealed class StaticOutboxScopeFactory(IProcessRuntimeOutboxStore outboxStore) : IServiceScopeFactory
    {
        public IServiceScope CreateScope()
        {
            return new StaticOutboxScope(outboxStore);
        }
    }

    private sealed class StaticOutboxScope(IProcessRuntimeOutboxStore outboxStore) : IServiceScope, IServiceProvider
    {
        public IServiceProvider ServiceProvider => this;

        public object? GetService(Type serviceType)
        {
            return serviceType == typeof(IProcessRuntimeOutboxStore) ? outboxStore : null;
        }

        public void Dispose()
        {
        }
    }

    private sealed class RecordingIntegrationEventPublisher : IIntegrationEventPublisher
    {
        public int PublishCalls { get; private set; }

        public List<IntegrationEnvelope> PublishedEnvelopes { get; } = [];

        public Task PublishAsync(IntegrationEnvelope envelope, CancellationToken cancellationToken = default)
        {
            PublishCalls += 1;
            PublishedEnvelopes.Add(envelope);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingIntegrationEventPublisher : IIntegrationEventPublisher
    {
        public int PublishCalls { get; private set; }

        public Task PublishAsync(IntegrationEnvelope envelope, CancellationToken cancellationToken = default)
        {
            PublishCalls += 1;
            throw new InvalidOperationException("simulated publish failure");
        }
    }

    private sealed class CancellationThrowingIntegrationEventPublisher : IIntegrationEventPublisher
    {
        public int PublishCalls { get; private set; }

        public Task PublishAsync(IntegrationEnvelope envelope, CancellationToken cancellationToken = default)
        {
            PublishCalls += 1;
            throw new OperationCanceledException(cancellationToken);
        }
    }

    private sealed class OperationCanceledIntegrationEventPublisher : IIntegrationEventPublisher
    {
        public int PublishCalls { get; private set; }

        public Task PublishAsync(IntegrationEnvelope envelope, CancellationToken cancellationToken = default)
        {
            PublishCalls += 1;
            throw new OperationCanceledException();
        }
    }

    private sealed class SequenceIntegrationEventPublisher : IIntegrationEventPublisher
    {
        public int PublishCalls { get; private set; }

        public bool ThrowOnFirstPublish { get; set; }

        public Task PublishAsync(IntegrationEnvelope envelope, CancellationToken cancellationToken = default)
        {
            PublishCalls += 1;

            if (ThrowOnFirstPublish && PublishCalls == 1)
            {
                throw new InvalidOperationException("simulated first publish failure");
            }

            return Task.CompletedTask;
        }
    }
}
