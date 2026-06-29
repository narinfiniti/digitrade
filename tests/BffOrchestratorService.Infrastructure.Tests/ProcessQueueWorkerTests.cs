using System.Reflection;
using BffOrchestratorService.Domain;
using BffOrchestratorService.Domain.Abstractions;
using BffOrchestratorService.Domain.Models;
using BffOrchestratorService.Infrastructure.Abstractions;
using BffOrchestratorService.Infrastructure.Options;
using BffOrchestratorService.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BffOrchestratorService.Infrastructure.Tests;

public sealed class ProcessQueueWorkerTests
{
    [Fact]
    public void ConstructorThrowsArgumentNullExceptionWhenServiceScopeFactoryIsNull()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new ProcessQueueWorkerOptions());

        Assert.Throws<ArgumentNullException>(() => new ProcessQueueWorker(
            null!,
            options,
            TimeProvider.System,
            NullLogger<ProcessQueueWorker>.Instance));
    }

    [Fact]
    public void ConstructorThrowsArgumentNullExceptionWhenOptionsIsNull()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var scopeFactory = services.GetRequiredService<IServiceScopeFactory>();

        Assert.Throws<ArgumentNullException>(() => new ProcessQueueWorker(
            scopeFactory,
            null!,
            TimeProvider.System,
            NullLogger<ProcessQueueWorker>.Instance));
    }

    [Fact]
    public void ConstructorThrowsArgumentNullExceptionWhenTimeProviderIsNull()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var scopeFactory = services.GetRequiredService<IServiceScopeFactory>();
        var options = Microsoft.Extensions.Options.Options.Create(new ProcessQueueWorkerOptions());

        Assert.Throws<ArgumentNullException>(() => new ProcessQueueWorker(
            scopeFactory,
            options,
            null!,
            NullLogger<ProcessQueueWorker>.Instance));
    }

    [Fact]
    public void ConstructorThrowsArgumentNullExceptionWhenLoggerIsNull()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var scopeFactory = services.GetRequiredService<IServiceScopeFactory>();
        var options = Microsoft.Extensions.Options.Options.Create(new ProcessQueueWorkerOptions());

        Assert.Throws<ArgumentNullException>(() => new ProcessQueueWorker(
            scopeFactory,
            options,
            TimeProvider.System,
            null!));
    }

    [Fact]
    public async Task ExecuteAsyncWhenWorkerDisabledDoesNotPollQueue()
    {
        var queueStore = new InMemoryProcessQueueStore();
        var worker = CreateWorker(queueStore, new[] { new PassiveHandler() }, enabled: false);

        await InvokeExecuteAsync(worker, CancellationToken.None);

        Assert.Equal(0, queueStore.LeaseReadyCalls);
        Assert.Equal(0, queueStore.RenewLeaseCalls);
        Assert.Equal(0, queueStore.CompleteCalls);
        Assert.Equal(0, queueStore.RequeueCalls);
        Assert.Equal(0, queueStore.DeadLetterCalls);
    }

    [Fact]
    public async Task ExecuteAsyncWhenNoHandlersRegisteredDoesNotPollQueue()
    {
        var queueStore = new InMemoryProcessQueueStore();
        var worker = CreateWorker(queueStore, Array.Empty<IProcessQueueWorkItemHandler>(), enabled: true);

        await InvokeExecuteAsync(worker, CancellationToken.None);

        Assert.Equal(0, queueStore.LeaseReadyCalls);
        Assert.Equal(0, queueStore.RenewLeaseCalls);
        Assert.Equal(0, queueStore.CompleteCalls);
        Assert.Equal(0, queueStore.RequeueCalls);
        Assert.Equal(0, queueStore.DeadLetterCalls);
    }

    [Fact]
    public async Task ExecuteAsyncWhenLeasePollingCancelledCompletesWithoutQueueMutations()
    {
        var queueStore = new InMemoryProcessQueueStore
        {
            ThrowCancellationOnLeaseReady = true,
        };

        var worker = CreateWorker(queueStore, new[] { new PassiveHandler() });

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await InvokeExecuteAsync(worker, cancellationTokenSource.Token);

        Assert.Equal(1, queueStore.LeaseReadyCalls);
        Assert.Equal(0, queueStore.CompleteCalls);
        Assert.Equal(0, queueStore.RequeueCalls);
        Assert.Equal(0, queueStore.DeadLetterCalls);
    }

    [Fact]
    public async Task ExecuteAsyncWhenTokenAlreadyCancelledPerformsInitialPollThenStops()
    {
        var queueStore = new InMemoryProcessQueueStore
        {
            LeaseReadyItems = [],
        };

        var worker = CreateWorker(queueStore, new[] { new PassiveHandler() }, enabled: true);

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await InvokeExecuteAsync(worker, cancellationTokenSource.Token);

        Assert.Equal(1, queueStore.LeaseReadyCalls);
        Assert.Equal(0, queueStore.RenewLeaseCalls);
        Assert.Equal(0, queueStore.CompleteCalls);
        Assert.Equal(0, queueStore.RequeueCalls);
        Assert.Equal(0, queueStore.DeadLetterCalls);
    }

    [Fact]
    public async Task PollOnceAsyncWhenNoItemsLeasedPerformsNoQueueMutations()
    {
        var queueStore = new InMemoryProcessQueueStore
        {
            LeaseReadyItems = [],
        };

        var worker = CreateWorker(queueStore, new[] { new PassiveHandler() });

        await InvokePollOnceAsync(worker, CancellationToken.None);

        Assert.Equal(1, queueStore.LeaseReadyCalls);
        Assert.Equal(0, queueStore.RenewLeaseCalls);
        Assert.Equal(0, queueStore.CompleteCalls);
        Assert.Equal(0, queueStore.RequeueCalls);
        Assert.Equal(0, queueStore.DeadLetterCalls);
    }

    [Fact]
    public async Task ExecuteAsyncWhenHandlerCancelledDoesNotCompleteOrRequeueWorkItem()
    {
        var queueStore = new InMemoryProcessQueueStore
        {
            LeaseReadyItems = [CreateReadyQueueItem(workType: "resume", queueItemId: 9001, attemptCount: 1, maxAttemptCount: 5)],
            RenewLeaseResult = true,
        };

        var worker = CreateWorker(queueStore, new[] { new CancellationThrowingHandler() });

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await InvokeExecuteAsync(worker, cancellationTokenSource.Token);

        Assert.Equal(1, queueStore.LeaseReadyCalls);
        Assert.Equal(1, queueStore.RenewLeaseCalls);
        Assert.Equal(0, queueStore.CompleteCalls);
        Assert.Equal(0, queueStore.RequeueCalls);
        Assert.Equal(0, queueStore.DeadLetterCalls);
    }

    [Fact]
    public async Task PollOnceAsyncWhenNoHandlerDeadLettersWorkItem()
    {
        var queueStore = new InMemoryProcessQueueStore
        {
            LeaseReadyItems = [CreateReadyQueueItem(workType: "unsupported_work", queueItemId: 9101, attemptCount: 1, maxAttemptCount: 5)],
            RenewLeaseResult = true,
        };

        var worker = CreateWorker(queueStore, new[] { new PassiveHandler() });

        await InvokePollOnceAsync(worker, CancellationToken.None);

        Assert.Equal(1, queueStore.LeaseReadyCalls);
        Assert.Equal(1, queueStore.RenewLeaseCalls);
        Assert.Equal(1, queueStore.DeadLetterCalls);
        Assert.Equal(0, queueStore.RequeueCalls);
        Assert.Equal(0, queueStore.CompleteCalls);
    }

    [Fact]
    public async Task PollOnceAsyncWhenHandlerFailsAndRetryAvailableRequeuesWorkItem()
    {
        var queueStore = new InMemoryProcessQueueStore
        {
            LeaseReadyItems = [CreateReadyQueueItem(workType: "resume", queueItemId: 9201, attemptCount: 1, maxAttemptCount: 5)],
            RenewLeaseResult = true,
        };

        var worker = CreateWorker(queueStore, new[] { new FaultThrowingHandler() });

        await InvokePollOnceAsync(worker, CancellationToken.None);

        Assert.Equal(1, queueStore.LeaseReadyCalls);
        Assert.Equal(1, queueStore.RenewLeaseCalls);
        Assert.Equal(1, queueStore.RequeueCalls);
        Assert.Equal(0, queueStore.DeadLetterCalls);
        Assert.Equal(0, queueStore.CompleteCalls);
    }

    [Fact]
    public async Task PollOnceAsyncWhenLeaseRenewalRejectedSkipsHandlerAndQueueMutations()
    {
        var queueStore = new InMemoryProcessQueueStore
        {
            LeaseReadyItems = [CreateReadyQueueItem(workType: "resume", queueItemId: 9251, attemptCount: 1, maxAttemptCount: 5)],
            RenewLeaseResult = false,
        };

        var handler = new RecordingHandler();
        var worker = CreateWorker(queueStore, new[] { handler });

        await InvokePollOnceAsync(worker, CancellationToken.None);

        Assert.Equal(1, queueStore.LeaseReadyCalls);
        Assert.Equal(1, queueStore.RenewLeaseCalls);
        Assert.Equal(0, handler.InvocationCount);
        Assert.Equal(0, queueStore.CompleteCalls);
        Assert.Equal(0, queueStore.RequeueCalls);
        Assert.Equal(0, queueStore.DeadLetterCalls);
    }

    [Fact]
    public async Task PollOnceAsyncWhenFirstLeaseRenewalRejectedContinuesProcessingRemainingLeasedItems()
    {
        var queueStore = new InMemoryProcessQueueStore
        {
            LeaseReadyItems =
            [
                CreateReadyQueueItem(workType: "resume", queueItemId: 9255, attemptCount: 1, maxAttemptCount: 5),
                CreateReadyQueueItem(workType: "resume", queueItemId: 9256, attemptCount: 1, maxAttemptCount: 5),
            ],
            RenewLeaseResult = false,
        };

        var handler = new RecordingHandler();
        var worker = CreateWorker(queueStore, new[] { handler });

        await InvokePollOnceAsync(worker, CancellationToken.None);

        Assert.Equal(1, queueStore.LeaseReadyCalls);
        Assert.Equal(2, queueStore.RenewLeaseCalls);
        Assert.Equal(0, handler.InvocationCount);
        Assert.Equal(0, queueStore.CompleteCalls);
        Assert.Equal(0, queueStore.RequeueCalls);
        Assert.Equal(0, queueStore.DeadLetterCalls);
    }

    [Fact]
    public async Task PollOnceAsyncWhenLeaseRenewalCancelledForShutdownDoesNotPropagateOrMutateQueue()
    {
        var queueStore = new InMemoryProcessQueueStore
        {
            LeaseReadyItems = [CreateReadyQueueItem(workType: "resume", queueItemId: 9252, attemptCount: 1, maxAttemptCount: 5)],
            ThrowCancellationOnRenewLease = true,
        };

        var handler = new RecordingHandler();
        var worker = CreateWorker(queueStore, new[] { handler });

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await InvokePollOnceAsync(worker, cancellationTokenSource.Token);

        Assert.Equal(1, queueStore.LeaseReadyCalls);
        Assert.Equal(1, queueStore.RenewLeaseCalls);
        Assert.Equal(0, handler.InvocationCount);
        Assert.Equal(0, queueStore.CompleteCalls);
        Assert.Equal(0, queueStore.RequeueCalls);
        Assert.Equal(0, queueStore.DeadLetterCalls);
    }

    [Fact]
    public async Task PollOnceAsyncWhenFirstLeaseRenewalCancelledBreaksBeforeProcessingRemainingLeasedItems()
    {
        var queueStore = new InMemoryProcessQueueStore
        {
            LeaseReadyItems =
            [
                CreateReadyQueueItem(workType: "resume", queueItemId: 9253, attemptCount: 1, maxAttemptCount: 5),
                CreateReadyQueueItem(workType: "resume", queueItemId: 9254, attemptCount: 1, maxAttemptCount: 5),
            ],
            ThrowCancellationOnRenewLease = true,
        };

        var handler = new RecordingHandler();
        var worker = CreateWorker(queueStore, new[] { handler });

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await InvokePollOnceAsync(worker, cancellationTokenSource.Token);

        Assert.Equal(1, queueStore.LeaseReadyCalls);
        Assert.Equal(1, queueStore.RenewLeaseCalls);
        Assert.Equal(0, handler.InvocationCount);
        Assert.Equal(0, queueStore.CompleteCalls);
        Assert.Equal(0, queueStore.RequeueCalls);
        Assert.Equal(0, queueStore.DeadLetterCalls);
    }

    [Fact]
    public async Task PollOnceAsyncWhenHandlerFailsAtRetryLimitDeadLettersWorkItem()
    {
        var queueStore = new InMemoryProcessQueueStore
        {
            LeaseReadyItems = [CreateReadyQueueItem(workType: "resume", queueItemId: 9301, attemptCount: 5, maxAttemptCount: 5)],
            RenewLeaseResult = true,
        };

        var worker = CreateWorker(queueStore, new[] { new FaultThrowingHandler() });

        await InvokePollOnceAsync(worker, CancellationToken.None);

        Assert.Equal(1, queueStore.LeaseReadyCalls);
        Assert.Equal(1, queueStore.RenewLeaseCalls);
        Assert.Equal(1, queueStore.DeadLetterCalls);
        Assert.Equal(0, queueStore.RequeueCalls);
        Assert.Equal(0, queueStore.CompleteCalls);
    }

    [Fact]
    public async Task PollOnceAsyncWhenHandlerSucceedsCompletesWorkItem()
    {
        var queueStore = new InMemoryProcessQueueStore
        {
            LeaseReadyItems = [CreateReadyQueueItem(workType: "resume", queueItemId: 9401, attemptCount: 1, maxAttemptCount: 5)],
            RenewLeaseResult = true,
        };

        var worker = CreateWorker(queueStore, new[] { new SuccessfulHandler() });

        await InvokePollOnceAsync(worker, CancellationToken.None);

        Assert.Equal(1, queueStore.LeaseReadyCalls);
        Assert.Equal(1, queueStore.RenewLeaseCalls);
        Assert.Equal(1, queueStore.CompleteCalls);
        Assert.Equal(0, queueStore.RequeueCalls);
        Assert.Equal(0, queueStore.DeadLetterCalls);
    }

    [Fact]
    public async Task PollOnceAsyncWhenFirstHandlerCannotHandleUsesNextMatchingHandler()
    {
        var queueStore = new InMemoryProcessQueueStore
        {
            LeaseReadyItems = [CreateReadyQueueItem(workType: "resume", queueItemId: 9402, attemptCount: 1, maxAttemptCount: 5)],
            RenewLeaseResult = true,
        };

        var nonMatchingHandler = new NonMatchingHandler();
        var matchingHandler = new RecordingHandler();
        var worker = CreateWorker(queueStore, new IProcessQueueWorkItemHandler[] { nonMatchingHandler, matchingHandler });

        await InvokePollOnceAsync(worker, CancellationToken.None);

        Assert.Equal(1, queueStore.LeaseReadyCalls);
        Assert.Equal(1, queueStore.RenewLeaseCalls);
        Assert.Equal(1, queueStore.CompleteCalls);
        Assert.Equal(0, queueStore.RequeueCalls);
        Assert.Equal(0, queueStore.DeadLetterCalls);
        Assert.Equal(0, nonMatchingHandler.InvocationCount);
        Assert.Equal(1, matchingHandler.InvocationCount);
    }

    [Fact]
    public async Task PollOnceAsyncWhenMultipleHandlersMatchUsesFirstMatchingHandlerOnly()
    {
        var queueStore = new InMemoryProcessQueueStore
        {
            LeaseReadyItems = [CreateReadyQueueItem(workType: "resume", queueItemId: 9403, attemptCount: 1, maxAttemptCount: 5)],
            RenewLeaseResult = true,
        };

        var firstMatchingHandler = new RecordingHandler();
        var secondMatchingHandler = new RecordingHandler();
        var worker = CreateWorker(queueStore, new IProcessQueueWorkItemHandler[] { firstMatchingHandler, secondMatchingHandler });

        await InvokePollOnceAsync(worker, CancellationToken.None);

        Assert.Equal(1, queueStore.LeaseReadyCalls);
        Assert.Equal(1, queueStore.RenewLeaseCalls);
        Assert.Equal(1, queueStore.CompleteCalls);
        Assert.Equal(0, queueStore.RequeueCalls);
        Assert.Equal(0, queueStore.DeadLetterCalls);
        Assert.Equal(1, firstMatchingHandler.InvocationCount);
        Assert.Equal(0, secondMatchingHandler.InvocationCount);
    }

    [Fact]
    public async Task PollOnceAsyncWhenDeadLetterCancelledForShutdownDoesNotPropagate()
    {
        var queueStore = new InMemoryProcessQueueStore
        {
            LeaseReadyItems = [CreateReadyQueueItem(workType: "unsupported_work", queueItemId: 9501, attemptCount: 1, maxAttemptCount: 5)],
            RenewLeaseResult = true,
            ThrowCancellationOnDeadLetter = true,
        };

        var worker = CreateWorker(queueStore, new[] { new PassiveHandler() });

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await InvokePollOnceAsync(worker, cancellationTokenSource.Token);

        Assert.Equal(1, queueStore.DeadLetterCalls);
        Assert.Equal(0, queueStore.CompleteCalls);
        Assert.Equal(0, queueStore.RequeueCalls);
    }

    [Fact]
    public async Task PollOnceAsyncWhenFirstMissingHandlerDeadLetterCancelledBreaksBeforeProcessingRemainingLeasedItems()
    {
        var queueStore = new InMemoryProcessQueueStore
        {
            LeaseReadyItems =
            [
                CreateReadyQueueItem(workType: "unsupported_work", queueItemId: 9502, attemptCount: 1, maxAttemptCount: 5),
                CreateReadyQueueItem(workType: "unsupported_work", queueItemId: 9503, attemptCount: 1, maxAttemptCount: 5),
            ],
            RenewLeaseResult = true,
            ThrowCancellationOnDeadLetter = true,
        };

        var worker = CreateWorker(queueStore, new[] { new PassiveHandler() });

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await InvokePollOnceAsync(worker, cancellationTokenSource.Token);

        Assert.Equal(1, queueStore.LeaseReadyCalls);
        Assert.Equal(1, queueStore.RenewLeaseCalls);
        Assert.Equal(1, queueStore.DeadLetterCalls);
        Assert.Equal(0, queueStore.RequeueCalls);
        Assert.Equal(0, queueStore.CompleteCalls);
    }

    [Fact]
    public async Task PollOnceAsyncWhenHandlerFailureDeadLetterCancelledForShutdownDoesNotPropagate()
    {
        var queueStore = new InMemoryProcessQueueStore
        {
            LeaseReadyItems = [CreateReadyQueueItem(workType: "resume", queueItemId: 9551, attemptCount: 5, maxAttemptCount: 5)],
            RenewLeaseResult = true,
            ThrowCancellationOnDeadLetter = true,
        };

        var worker = CreateWorker(queueStore, new[] { new FaultThrowingHandler() });

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await InvokePollOnceAsync(worker, cancellationTokenSource.Token);

        Assert.Equal(1, queueStore.LeaseReadyCalls);
        Assert.Equal(1, queueStore.RenewLeaseCalls);
        Assert.Equal(1, queueStore.DeadLetterCalls);
        Assert.Equal(0, queueStore.RequeueCalls);
        Assert.Equal(0, queueStore.CompleteCalls);
    }

    [Fact]
    public async Task PollOnceAsyncWhenFirstHandlerFailureDeadLetterCancelledBreaksBeforeProcessingRemainingLeasedItems()
    {
        var queueStore = new InMemoryProcessQueueStore
        {
            LeaseReadyItems =
            [
                CreateReadyQueueItem(workType: "resume", queueItemId: 9552, attemptCount: 5, maxAttemptCount: 5),
                CreateReadyQueueItem(workType: "resume", queueItemId: 9553, attemptCount: 5, maxAttemptCount: 5),
            ],
            RenewLeaseResult = true,
            ThrowCancellationOnDeadLetter = true,
        };

        var worker = CreateWorker(queueStore, new[] { new FaultThrowingHandler() });

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await InvokePollOnceAsync(worker, cancellationTokenSource.Token);

        Assert.Equal(1, queueStore.LeaseReadyCalls);
        Assert.Equal(1, queueStore.RenewLeaseCalls);
        Assert.Equal(1, queueStore.DeadLetterCalls);
        Assert.Equal(0, queueStore.RequeueCalls);
        Assert.Equal(0, queueStore.CompleteCalls);
    }

    [Fact]
    public async Task PollOnceAsyncWhenRequeueCancelledForShutdownDoesNotPropagate()
    {
        var queueStore = new InMemoryProcessQueueStore
        {
            LeaseReadyItems = [CreateReadyQueueItem(workType: "resume", queueItemId: 9601, attemptCount: 1, maxAttemptCount: 5)],
            RenewLeaseResult = true,
            ThrowCancellationOnRequeue = true,
        };

        var worker = CreateWorker(queueStore, new[] { new FaultThrowingHandler() });

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await InvokePollOnceAsync(worker, cancellationTokenSource.Token);

        Assert.Equal(1, queueStore.RequeueCalls);
        Assert.Equal(0, queueStore.DeadLetterCalls);
        Assert.Equal(0, queueStore.CompleteCalls);
    }

    [Fact]
    public async Task PollOnceAsyncWhenFirstRequeueCancelledBreaksBeforeProcessingRemainingLeasedItems()
    {
        var queueStore = new InMemoryProcessQueueStore
        {
            LeaseReadyItems =
            [
                CreateReadyQueueItem(workType: "resume", queueItemId: 9602, attemptCount: 1, maxAttemptCount: 5),
                CreateReadyQueueItem(workType: "resume", queueItemId: 9603, attemptCount: 1, maxAttemptCount: 5),
            ],
            RenewLeaseResult = true,
            ThrowCancellationOnRequeue = true,
        };

        var worker = CreateWorker(queueStore, new[] { new FaultThrowingHandler() });

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await InvokePollOnceAsync(worker, cancellationTokenSource.Token);

        Assert.Equal(1, queueStore.LeaseReadyCalls);
        Assert.Equal(1, queueStore.RenewLeaseCalls);
        Assert.Equal(1, queueStore.RequeueCalls);
        Assert.Equal(0, queueStore.DeadLetterCalls);
        Assert.Equal(0, queueStore.CompleteCalls);
    }

    [Fact]
    public async Task PollOnceAsyncWhenCompleteCancelledForShutdownDoesNotPropagate()
    {
        var queueStore = new InMemoryProcessQueueStore
        {
            LeaseReadyItems = [CreateReadyQueueItem(workType: "resume", queueItemId: 9701, attemptCount: 1, maxAttemptCount: 5)],
            RenewLeaseResult = true,
            ThrowCancellationOnComplete = true,
        };

        var worker = CreateWorker(queueStore, new[] { new SuccessfulHandler() });

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await InvokePollOnceAsync(worker, cancellationTokenSource.Token);

        Assert.Equal(1, queueStore.CompleteCalls);
        Assert.Equal(0, queueStore.RequeueCalls);
        Assert.Equal(0, queueStore.DeadLetterCalls);
    }

    [Fact]
    public async Task PollOnceAsyncWhenFirstCompletionCancelledBreaksBeforeProcessingRemainingLeasedItems()
    {
        var queueStore = new InMemoryProcessQueueStore
        {
            LeaseReadyItems =
            [
                CreateReadyQueueItem(workType: "resume", queueItemId: 9702, attemptCount: 1, maxAttemptCount: 5),
                CreateReadyQueueItem(workType: "resume", queueItemId: 9703, attemptCount: 1, maxAttemptCount: 5),
            ],
            RenewLeaseResult = true,
            ThrowCancellationOnComplete = true,
        };

        var worker = CreateWorker(queueStore, new[] { new SuccessfulHandler() });

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await InvokePollOnceAsync(worker, cancellationTokenSource.Token);

        Assert.Equal(1, queueStore.LeaseReadyCalls);
        Assert.Equal(1, queueStore.RenewLeaseCalls);
        Assert.Equal(1, queueStore.CompleteCalls);
        Assert.Equal(0, queueStore.RequeueCalls);
        Assert.Equal(0, queueStore.DeadLetterCalls);
    }

    [Fact]
    public async Task PollOnceAsyncWhenDeadLetterRejectedDoesNotRequeueOrComplete()
    {
        var queueStore = new InMemoryProcessQueueStore
        {
            LeaseReadyItems = [CreateReadyQueueItem(workType: "unsupported_work", queueItemId: 9801, attemptCount: 1, maxAttemptCount: 5)],
            RenewLeaseResult = true,
            DeadLetterResult = false,
        };

        var worker = CreateWorker(queueStore, new[] { new PassiveHandler() });

        await InvokePollOnceAsync(worker, CancellationToken.None);

        Assert.Equal(1, queueStore.DeadLetterCalls);
        Assert.Equal(0, queueStore.RequeueCalls);
        Assert.Equal(0, queueStore.CompleteCalls);
    }

    [Fact]
    public async Task PollOnceAsyncWhenFirstHandlerFailureDeadLetterRejectedContinuesProcessingRemainingLeasedItems()
    {
        var queueStore = new InMemoryProcessQueueStore
        {
            LeaseReadyItems =
            [
                CreateReadyQueueItem(workType: "resume", queueItemId: 9810, attemptCount: 5, maxAttemptCount: 5),
                CreateReadyQueueItem(workType: "resume", queueItemId: 9811, attemptCount: 5, maxAttemptCount: 5),
            ],
            RenewLeaseResult = true,
            DeadLetterResult = false,
        };

        var worker = CreateWorker(queueStore, new[] { new FaultThrowingHandler() });

        await InvokePollOnceAsync(worker, CancellationToken.None);

        Assert.Equal(1, queueStore.LeaseReadyCalls);
        Assert.Equal(2, queueStore.RenewLeaseCalls);
        Assert.Equal(2, queueStore.DeadLetterCalls);
        Assert.Equal(0, queueStore.RequeueCalls);
        Assert.Equal(0, queueStore.CompleteCalls);
    }

    [Fact]
    public async Task PollOnceAsyncWhenFirstMissingHandlerDeadLetterRejectedContinuesProcessingRemainingLeasedItems()
    {
        var queueStore = new InMemoryProcessQueueStore
        {
            LeaseReadyItems =
            [
                CreateReadyQueueItem(workType: "unsupported_work", queueItemId: 9808, attemptCount: 1, maxAttemptCount: 5),
                CreateReadyQueueItem(workType: "unsupported_work", queueItemId: 9809, attemptCount: 1, maxAttemptCount: 5),
            ],
            RenewLeaseResult = true,
            DeadLetterResult = false,
        };

        var worker = CreateWorker(queueStore, new[] { new PassiveHandler() });

        await InvokePollOnceAsync(worker, CancellationToken.None);

        Assert.Equal(1, queueStore.LeaseReadyCalls);
        Assert.Equal(2, queueStore.RenewLeaseCalls);
        Assert.Equal(2, queueStore.DeadLetterCalls);
        Assert.Equal(0, queueStore.RequeueCalls);
        Assert.Equal(0, queueStore.CompleteCalls);
    }

    [Fact]
    public async Task PollOnceAsyncWhenRequeueRejectedDoesNotDeadLetterOrComplete()
    {
        var queueStore = new InMemoryProcessQueueStore
        {
            LeaseReadyItems = [CreateReadyQueueItem(workType: "resume", queueItemId: 9802, attemptCount: 1, maxAttemptCount: 5)],
            RenewLeaseResult = true,
            RequeueResult = false,
        };

        var worker = CreateWorker(queueStore, new[] { new FaultThrowingHandler() });

        await InvokePollOnceAsync(worker, CancellationToken.None);

        Assert.Equal(1, queueStore.RequeueCalls);
        Assert.Equal(0, queueStore.DeadLetterCalls);
        Assert.Equal(0, queueStore.CompleteCalls);
    }

    [Fact]
    public async Task PollOnceAsyncWhenFirstRequeueRejectedContinuesProcessingRemainingLeasedItems()
    {
        var queueStore = new InMemoryProcessQueueStore
        {
            LeaseReadyItems =
            [
                CreateReadyQueueItem(workType: "resume", queueItemId: 9806, attemptCount: 1, maxAttemptCount: 5),
                CreateReadyQueueItem(workType: "resume", queueItemId: 9807, attemptCount: 1, maxAttemptCount: 5),
            ],
            RenewLeaseResult = true,
            RequeueResult = false,
        };

        var worker = CreateWorker(queueStore, new[] { new FaultThrowingHandler() });

        await InvokePollOnceAsync(worker, CancellationToken.None);

        Assert.Equal(1, queueStore.LeaseReadyCalls);
        Assert.Equal(2, queueStore.RenewLeaseCalls);
        Assert.Equal(2, queueStore.RequeueCalls);
        Assert.Equal(0, queueStore.DeadLetterCalls);
        Assert.Equal(0, queueStore.CompleteCalls);
    }

    [Fact]
    public async Task PollOnceAsyncWhenCompleteRejectedDoesNotRequeueOrDeadLetter()
    {
        var queueStore = new InMemoryProcessQueueStore
        {
            LeaseReadyItems = [CreateReadyQueueItem(workType: "resume", queueItemId: 9803, attemptCount: 1, maxAttemptCount: 5)],
            RenewLeaseResult = true,
            CompleteResult = false,
        };

        var worker = CreateWorker(queueStore, new[] { new SuccessfulHandler() });

        await InvokePollOnceAsync(worker, CancellationToken.None);

        Assert.Equal(1, queueStore.CompleteCalls);
        Assert.Equal(0, queueStore.RequeueCalls);
        Assert.Equal(0, queueStore.DeadLetterCalls);
    }

    [Fact]
    public void ComputeNextVisibleAtWhenAttemptCountIsHighUsesCappedExponentDelayWindowWithJitter()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var scopeFactory = services.GetRequiredService<IServiceScopeFactory>();
        var options = Microsoft.Extensions.Options.Options.Create(new ProcessQueueWorkerOptions
        {
            Enabled = true,
            BatchSize = 10,
            PollInterval = TimeSpan.FromMilliseconds(25),
            LeaseDuration = TimeSpan.FromSeconds(5),
            LeaseOwner = "test-worker",
        });

        var worker = new ProcessQueueWorker(
            scopeFactory,
            options,
            TimeProvider.System,
            NullLogger<ProcessQueueWorker>.Instance);

        var computeNextVisibleAtMethod = typeof(ProcessQueueWorker).GetMethod("ComputeNextVisibleAt", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not find ComputeNextVisibleAt method.");

        var observedAt = new DateTimeOffset(2026, 5, 29, 0, 0, 0, TimeSpan.Zero);
        var queueItem = CreateReadyQueueItem(workType: "resume", queueItemId: 9901, attemptCount: 1_000, maxAttemptCount: 1_000);

        var nextVisibleAt = (DateTimeOffset)computeNextVisibleAtMethod.Invoke(worker, new object[] { queueItem, observedAt })!;
        var delay = nextVisibleAt - observedAt;

        Assert.InRange(delay, TimeSpan.FromMilliseconds(5120), TimeSpan.FromMilliseconds(7680));
    }

    [Fact]
    public void ComputeNextVisibleAtWhenAttemptCountIsZeroStillUsesAtLeastPollInterval()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var scopeFactory = services.GetRequiredService<IServiceScopeFactory>();
        var options = Microsoft.Extensions.Options.Options.Create(new ProcessQueueWorkerOptions
        {
            Enabled = true,
            BatchSize = 10,
            PollInterval = TimeSpan.FromMilliseconds(25),
            LeaseDuration = TimeSpan.FromSeconds(5),
            LeaseOwner = "test-worker",
        });

        var worker = new ProcessQueueWorker(
            scopeFactory,
            options,
            TimeProvider.System,
            NullLogger<ProcessQueueWorker>.Instance);

        var computeNextVisibleAtMethod = typeof(ProcessQueueWorker).GetMethod("ComputeNextVisibleAt", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not find ComputeNextVisibleAt method.");

        var observedAt = new DateTimeOffset(2026, 5, 29, 0, 0, 0, TimeSpan.Zero);
        var queueItem = CreateReadyQueueItem(workType: "resume", queueItemId: 9902, attemptCount: 0, maxAttemptCount: 5);

        var nextVisibleAt = (DateTimeOffset)computeNextVisibleAtMethod.Invoke(worker, new object[] { queueItem, observedAt })!;
        var delay = nextVisibleAt - observedAt;

        Assert.True(delay >= TimeSpan.FromMilliseconds(25));
        Assert.InRange(delay, TimeSpan.FromMilliseconds(25), TimeSpan.FromMilliseconds(30));
    }

    [Fact]
    public async Task PollOnceAsyncWhenFirstCompletionRejectedContinuesProcessingRemainingLeasedItems()
    {
        var queueStore = new InMemoryProcessQueueStore
        {
            LeaseReadyItems =
            [
                CreateReadyQueueItem(workType: "resume", queueItemId: 9804, attemptCount: 1, maxAttemptCount: 5),
                CreateReadyQueueItem(workType: "resume", queueItemId: 9805, attemptCount: 1, maxAttemptCount: 5),
            ],
            RenewLeaseResult = true,
            CompleteResult = false,
        };

        var worker = CreateWorker(queueStore, new[] { new SuccessfulHandler() });

        await InvokePollOnceAsync(worker, CancellationToken.None);

        Assert.Equal(1, queueStore.LeaseReadyCalls);
        Assert.Equal(2, queueStore.RenewLeaseCalls);
        Assert.Equal(2, queueStore.CompleteCalls);
        Assert.Equal(0, queueStore.RequeueCalls);
        Assert.Equal(0, queueStore.DeadLetterCalls);
    }

    private static ProcessQueueWorker CreateWorker(
        IProcessQueueStore queueStore,
        IEnumerable<IProcessQueueWorkItemHandler> handlers,
        bool enabled = true)
    {
        var services = new ServiceCollection();
        services.AddSingleton(queueStore);

        foreach (var handler in handlers)
        {
            services.AddSingleton(handler);
            services.AddSingleton<IProcessQueueWorkItemHandler>(handler);
        }

        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        var options = Microsoft.Extensions.Options.Options.Create(new ProcessQueueWorkerOptions
        {
            Enabled = enabled,
            BatchSize = 10,
            PollInterval = TimeSpan.FromMilliseconds(25),
            LeaseDuration = TimeSpan.FromSeconds(5),
            LeaseOwner = "test-worker",
        });

        return new ProcessQueueWorker(
            scopeFactory,
            options,
            TimeProvider.System,
            NullLogger<ProcessQueueWorker>.Instance);
    }

    private static async Task InvokeExecuteAsync(ProcessQueueWorker worker, CancellationToken cancellationToken)
    {
        var executeAsyncMethod = typeof(ProcessQueueWorker).GetMethod("ExecuteAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not find ExecuteAsync method.");

        var task = executeAsyncMethod.Invoke(worker, new object[] { cancellationToken }) as Task
            ?? throw new InvalidOperationException("ExecuteAsync invocation did not return a Task.");

        await task;
    }

    private static async Task InvokePollOnceAsync(ProcessQueueWorker worker, CancellationToken cancellationToken)
    {
        var pollOnceAsyncMethod = typeof(ProcessQueueWorker).GetMethod("PollOnceAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Could not find PollOnceAsync method.");

        var task = pollOnceAsyncMethod.Invoke(worker, new object[] { cancellationToken }) as Task
            ?? throw new InvalidOperationException("PollOnceAsync invocation did not return a Task.");

        await task;
    }

    private static ProcessQueueItemModel CreateReadyQueueItem(string workType, long queueItemId, int attemptCount, int maxAttemptCount)
    {
        return new ProcessQueueItemModel
        {
            Id = queueItemId,
            ProcessId = Guid.NewGuid(),
            ProcessKey = "trade:account-1",
            FlowType = "synchronous",
            WorkType = workType,
            Status = "ready",
            Priority = 100,
            VisibleAt = TimeProvider.System.GetUtcNow(),
            SequenceNo = 1,
            AttemptCount = attemptCount,
            MaxAttemptCount = maxAttemptCount,
            DedupeKey = $"test:{queueItemId}",
            Payload = "{}",
            CreatedAt = TimeProvider.System.GetUtcNow(),
            UpdatedAt = TimeProvider.System.GetUtcNow(),
        };
    }

    private sealed class PassiveHandler : IProcessQueueWorkItemHandler
    {
        public bool CanHandle(ProcessQueueItemModel queueItem)
        {
            return false;
        }

        public Task HandleAsync(ProcessQueueItemModel queueItem, string leaseOwner, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class CancellationThrowingHandler : IProcessQueueWorkItemHandler
    {
        public bool CanHandle(ProcessQueueItemModel queueItem)
        {
            return true;
        }

        public Task HandleAsync(ProcessQueueItemModel queueItem, string leaseOwner, CancellationToken cancellationToken = default)
        {
            throw new OperationCanceledException(cancellationToken);
        }
    }

    private sealed class FaultThrowingHandler : IProcessQueueWorkItemHandler
    {
        public bool CanHandle(ProcessQueueItemModel queueItem)
        {
            return true;
        }

        public Task HandleAsync(ProcessQueueItemModel queueItem, string leaseOwner, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("simulated handler failure");
        }
    }

    private sealed class SuccessfulHandler : IProcessQueueWorkItemHandler
    {
        public bool CanHandle(ProcessQueueItemModel queueItem)
        {
            return true;
        }

        public Task HandleAsync(ProcessQueueItemModel queueItem, string leaseOwner, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingHandler : IProcessQueueWorkItemHandler
    {
        public int InvocationCount { get; private set; }

        public bool CanHandle(ProcessQueueItemModel queueItem)
        {
            return true;
        }

        public Task HandleAsync(ProcessQueueItemModel queueItem, string leaseOwner, CancellationToken cancellationToken = default)
        {
            InvocationCount += 1;
            return Task.CompletedTask;
        }
    }

    private sealed class NonMatchingHandler : IProcessQueueWorkItemHandler
    {
        public int InvocationCount { get; private set; }

        public bool CanHandle(ProcessQueueItemModel queueItem)
        {
            return false;
        }

        public Task HandleAsync(ProcessQueueItemModel queueItem, string leaseOwner, CancellationToken cancellationToken = default)
        {
            InvocationCount += 1;
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryProcessQueueStore : IProcessQueueStore
    {
        public int LeaseReadyCalls { get; private set; }

        public int RenewLeaseCalls { get; private set; }

        public int CompleteCalls { get; private set; }

        public int RequeueCalls { get; private set; }

        public int DeadLetterCalls { get; private set; }

        public bool ThrowCancellationOnLeaseReady { get; set; }

        public bool ThrowCancellationOnDeadLetter { get; set; }

        public bool ThrowCancellationOnRequeue { get; set; }

        public bool ThrowCancellationOnComplete { get; set; }

        public bool ThrowCancellationOnRenewLease { get; set; }

        public bool DeadLetterResult { get; set; } = true;

        public bool RequeueResult { get; set; } = true;

        public bool CompleteResult { get; set; } = true;

        public bool RenewLeaseResult { get; set; }

        public List<ProcessQueueItemModel> LeaseReadyItems { get; init; } = [];

        public Task AddAsync(ProcessQueueItemModel queueItem, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task UpdateAsync(ProcessQueueItemModel queueItem, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<ProcessQueueItemModel?> FindByDedupeKeyAsync(string dedupeKey, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ProcessQueueItemModel?>(null);
        }

        public Task<IReadOnlyCollection<ProcessQueueItemModel>> LeaseReadyAsync(int batchSize, DateTimeOffset asOfUtc, string leaseOwner, TimeSpan leaseDuration, CancellationToken cancellationToken = default)
        {
            LeaseReadyCalls += 1;

            if (ThrowCancellationOnLeaseReady)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            IReadOnlyCollection<ProcessQueueItemModel> result = LeaseReadyItems.ToArray();
            return Task.FromResult(result);
        }

        public Task<bool> RenewLeaseAsync(long queueItemId, DateTimeOffset asOfUtc, string leaseOwner, TimeSpan leaseDuration, CancellationToken cancellationToken = default)
        {
            RenewLeaseCalls += 1;

            if (ThrowCancellationOnRenewLease)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            return Task.FromResult(RenewLeaseResult);
        }

        public Task<bool> CompleteAsync(long queueItemId, DateTimeOffset asOfUtc, string leaseOwner, CancellationToken cancellationToken = default)
        {
            CompleteCalls += 1;

            if (ThrowCancellationOnComplete)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            return Task.FromResult(CompleteResult);
        }

        public Task<bool> DeadLetterAsync(long queueItemId, DateTimeOffset asOfUtc, string leaseOwner, string errorCode, string errorMessage, CancellationToken cancellationToken = default)
        {
            DeadLetterCalls += 1;

            if (ThrowCancellationOnDeadLetter)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            return Task.FromResult(DeadLetterResult);
        }

        public Task<bool> RequeueAsync(long queueItemId, DateTimeOffset asOfUtc, DateTimeOffset visibleAt, string leaseOwner, string errorCode, string errorMessage, CancellationToken cancellationToken = default)
        {
            RequeueCalls += 1;

            if (ThrowCancellationOnRequeue)
            {
                throw new OperationCanceledException(cancellationToken);
            }

            return Task.FromResult(RequeueResult);
        }

        public Task<IReadOnlyCollection<ProcessQueueItemModel>> ListVisibleAsync(int batchSize, DateTimeOffset asOfUtc, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(1);
        }
    }
}
