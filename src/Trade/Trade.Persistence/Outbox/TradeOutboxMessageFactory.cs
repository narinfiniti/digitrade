using System.Text.Json;
using DigiTrade.Messaging.Persistence.Outbox;
using DigiTrade.SharedKernel.Events;
using Trade.Application.Events;
using Trade.Domain.Trades.Events;

namespace Trade.Persistence.Outbox;

internal static class TradeOutboxMessageFactory
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static OutboxMessage Create(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        return domainEvent switch
        {
            TradeOpenedDomainEvent tradeOpened => CreateTradeOpenedMessage(tradeOpened),
            TradeClosedDomainEvent tradeClosed => CreateTradeClosedMessage(tradeClosed),
            _ => throw new InvalidOperationException($"Trade outbox does not support domain event '{domainEvent.GetType().Name}'."),
        };
    }

    private static OutboxMessage CreateTradeOpenedMessage(TradeOpenedDomainEvent domainEvent)
    {
        var integrationEvent = new TradeOpenedIntegrationEvent(
            domainEvent.EventId,
            domainEvent.TradeId.ToString("D"),
            domainEvent.AccountId,
            domainEvent.InstrumentId,
            domainEvent.Direction,
            domainEvent.Quantity,
            domainEvent.OpenPrice,
            domainEvent.OccurredAtUtc);

        return CreateMessage(integrationEvent);
    }

    private static OutboxMessage CreateTradeClosedMessage(TradeClosedDomainEvent domainEvent)
    {
        var integrationEvent = new TradeClosedIntegrationEvent(
            domainEvent.EventId,
            domainEvent.TradeId.ToString("D"),
            domainEvent.ClosePrice,
            domainEvent.OccurredAtUtc);

        return CreateMessage(integrationEvent);
    }

    private static OutboxMessage CreateMessage<TIntegrationEvent>(TIntegrationEvent integrationEvent)
        where TIntegrationEvent : class, DigiTrade.Messaging.Contracts.IIntegrationEvent
    {
        return new OutboxMessage(
            Guid.NewGuid(),
            integrationEvent.EventId,
            integrationEvent.EventName,
            integrationEvent.AggregateId,
            integrationEvent.AggregateId,
            integrationEvent.EventVersion,
            integrationEvent.OccurredAtUtc,
            JsonSerializer.Serialize(integrationEvent, SerializerOptions),
            null,
            null,
            OutboxMessageStatus.Pending,
            0,
            null,
            null,
            null);
    }
}