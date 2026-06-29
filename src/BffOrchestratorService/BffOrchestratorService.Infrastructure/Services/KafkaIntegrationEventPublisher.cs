using System.Text;
using System.Text.Json;
using BffOrchestratorService.Infrastructure.Options;
using Confluent.Kafka;
using DigiTrade.Messaging.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BffOrchestratorService.Infrastructure.Services;

public sealed partial class KafkaIntegrationEventPublisher : IIntegrationEventPublisher, IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IProducer<string, byte[]> producer;
    private readonly KafkaIntegrationPublisherOptions options;
    private readonly ILogger<KafkaIntegrationEventPublisher> logger;
    private bool disposed;

    public KafkaIntegrationEventPublisher(
        IOptions<KafkaIntegrationPublisherOptions> options,
        ILogger<KafkaIntegrationEventPublisher> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        this.options = options.Value;
        this.logger = logger;

        producer = new ProducerBuilder<string, byte[]>(new ProducerConfig
        {
            BootstrapServers = this.options.BootstrapServers,
            ClientId = this.options.ClientId,
            EnableIdempotence = this.options.EnableIdempotence,
            Acks = Acks.All,
            MessageSendMaxRetries = 3,
            RetryBackoffMs = 250,
        }).Build();
    }

    public async Task PublishAsync(IntegrationEnvelope envelope, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();

        var brokerMessage = new BrokerIntegrationEnvelope(
            envelope.IntegrationEvent.EventName,
            envelope.IntegrationEvent.EventId,
            envelope.IntegrationEvent.EventVersion,
            envelope.IntegrationEvent.AggregateId,
            envelope.IntegrationEvent.OccurredAtUtc,
            envelope.PartitionKey,
            envelope.CorrelationId,
            envelope.Headers,
            envelope.IntegrationEvent);

        var message = new Message<string, byte[]>
        {
            Key = envelope.PartitionKey,
            Value = JsonSerializer.SerializeToUtf8Bytes(brokerMessage, SerializerOptions),
            Headers = BuildHeaders(envelope),
            Timestamp = new Timestamp(envelope.IntegrationEvent.OccurredAtUtc.UtcDateTime),
        };

        var deliveryResult = await producer.ProduceAsync(options.TopicName, message, cancellationToken);

        LogPublished(
            logger,
            options.TopicName,
            deliveryResult.Partition.Value,
            deliveryResult.Offset.Value,
            envelope.IntegrationEvent.EventName,
            envelope.IntegrationEvent.EventId,
            envelope.PartitionKey,
            envelope.CorrelationId,
            null);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        producer.Flush(TimeSpan.FromSeconds(10));
        producer.Dispose();
        disposed = true;
    }

    private static Headers BuildHeaders(IntegrationEnvelope envelope)
    {
        var headers = new Headers();

        foreach (var header in envelope.Headers)
        {
            headers.Add(header.Key, Encoding.UTF8.GetBytes(header.Value));
        }

        if (!string.IsNullOrWhiteSpace(envelope.CorrelationId)
            && !envelope.Headers.ContainsKey("correlation-id"))
        {
            headers.Add("correlation-id", Encoding.UTF8.GetBytes(envelope.CorrelationId));
        }

        headers.Add("content-type", Encoding.UTF8.GetBytes("application/json"));

        return headers;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }

    private sealed record BrokerIntegrationEnvelope(
        string EventName,
        Guid EventId,
        int EventVersion,
        string AggregateId,
        DateTimeOffset OccurredAtUtc,
        string PartitionKey,
        string? CorrelationId,
        IReadOnlyDictionary<string, string> Headers,
        object IntegrationEvent);

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Published integration event {EventName} ({EventId}) to topic {TopicName}, partition {Partition}, offset {Offset}, key {PartitionKey}, correlation {CorrelationId}.")]
    private static partial void LogPublished(
        ILogger logger,
        string topicName,
        int partition,
        long offset,
        string eventName,
        Guid eventId,
        string partitionKey,
        string? correlationId,
        Exception? exception);
}