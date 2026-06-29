using System.Collections.Concurrent;
using DigiTrade.Messaging.Contracts;

namespace Order.Tests;

public sealed class TestIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly ConcurrentQueue<IntegrationEnvelope> publishedEnvelopes = new();

    public IReadOnlyCollection<IntegrationEnvelope> PublishedEnvelopes => publishedEnvelopes.ToArray();

    public Task PublishAsync(IntegrationEnvelope envelope, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        publishedEnvelopes.Enqueue(envelope);
        return Task.CompletedTask;
    }

    public void Clear()
    {
        while (publishedEnvelopes.TryDequeue(out _))
        {
        }
    }
}