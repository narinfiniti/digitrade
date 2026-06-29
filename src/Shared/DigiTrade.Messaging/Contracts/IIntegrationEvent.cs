namespace DigiTrade.Messaging.Contracts;

public interface IIntegrationEvent
{
    Guid EventId { get; }

    string EventName { get; }

    int EventVersion { get; }

    string AggregateId { get; }

    DateTimeOffset OccurredAtUtc { get; }
}