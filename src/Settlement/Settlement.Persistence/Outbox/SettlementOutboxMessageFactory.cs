using System.Text.Json;
using DigiTrade.Messaging.Persistence.Outbox;
using DigiTrade.SharedKernel.Events;
using Settlement.Application.Events;
using Settlement.Domain.Settlements.Events;

namespace Settlement.Persistence.Outbox;

internal static class SettlementOutboxMessageFactory
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static OutboxMessage Create(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        return domainEvent switch
        {
            SettlementInitiatedDomainEvent settlementInitiated => CreateSettlementInitiatedMessage(settlementInitiated),
            SettlementFinalizedDomainEvent settlementFinalized => CreateSettlementFinalizedMessage(settlementFinalized),
            SettlementFailedDomainEvent settlementFailed => CreateSettlementFailedMessage(settlementFailed),
            _ => throw new InvalidOperationException($"Settlement outbox does not support domain event '{domainEvent.GetType().Name}'."),
        };
    }

    private static OutboxMessage CreateSettlementInitiatedMessage(SettlementInitiatedDomainEvent domainEvent)
    {
        var integrationEvent = new SettlementInitiatedIntegrationEvent(
            domainEvent.EventId,
            domainEvent.SettlementId.ToString("D"),
            domainEvent.TradeId.ToString("D"),
            domainEvent.AccountId,
            domainEvent.CurrencyCode,
            domainEvent.NetAmount,
            domainEvent.OccurredAtUtc);

        return CreateMessage(integrationEvent);
    }

    private static OutboxMessage CreateSettlementFinalizedMessage(SettlementFinalizedDomainEvent domainEvent)
    {
        var integrationEvent = new SettlementFinalizedIntegrationEvent(
            domainEvent.EventId,
            domainEvent.SettlementId.ToString("D"),
            domainEvent.TradeId.ToString("D"),
            domainEvent.AccountId,
            domainEvent.CurrencyCode,
            domainEvent.NetAmount,
            domainEvent.OccurredAtUtc);

        return CreateMessage(integrationEvent);
    }

    private static OutboxMessage CreateSettlementFailedMessage(SettlementFailedDomainEvent domainEvent)
    {
        var integrationEvent = new SettlementFailedIntegrationEvent(
            domainEvent.EventId,
            domainEvent.SettlementId.ToString("D"),
            domainEvent.TradeId.ToString("D"),
            domainEvent.FailureReason,
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