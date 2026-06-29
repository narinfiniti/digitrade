namespace DigiTrade.Messaging.Contracts;

public interface IIntegrationEventConsumer<in TEvent>
    where TEvent : IIntegrationEvent
{
    Task ConsumeAsync(TEvent integrationEvent, CancellationToken cancellationToken = default);
}