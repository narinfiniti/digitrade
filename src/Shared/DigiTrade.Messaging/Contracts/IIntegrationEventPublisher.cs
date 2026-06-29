namespace DigiTrade.Messaging.Contracts;

public interface IIntegrationEventPublisher
{
    Task PublishAsync(IntegrationEnvelope envelope, CancellationToken cancellationToken = default);
}