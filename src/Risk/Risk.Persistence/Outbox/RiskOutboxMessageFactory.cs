using System.Text.Json;
using DigiTrade.Messaging.Persistence.Outbox;
using DigiTrade.SharedKernel.Events;
using Risk.Application.Events;
using Risk.Domain.Margins.Events;

namespace Risk.Persistence.Outbox;

internal static class RiskOutboxMessageFactory
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static OutboxMessage Create(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        return domainEvent switch
        {
            MarginAccountOpenedDomainEvent accountOpened => CreateMarginAccountOpenedMessage(accountOpened),
            MarginReservedDomainEvent marginReserved => CreateMarginReservedMessage(marginReserved),
            MarginReleasedDomainEvent marginReleased => CreateMarginReleasedMessage(marginReleased),
            _ => throw new InvalidOperationException($"Risk outbox does not support domain event '{domainEvent.GetType().Name}'."),
        };
    }

    private static OutboxMessage CreateMarginAccountOpenedMessage(MarginAccountOpenedDomainEvent domainEvent)
    {
        var integrationEvent = new MarginAccountOpenedIntegrationEvent(
            domainEvent.EventId,
            domainEvent.MarginAccountId.ToString("D"),
            domainEvent.AccountId,
            domainEvent.CurrencyCode,
            domainEvent.TotalMargin,
            domainEvent.OccurredAtUtc);

        return CreateMessage(integrationEvent);
    }

    private static OutboxMessage CreateMarginReservedMessage(MarginReservedDomainEvent domainEvent)
    {
        var integrationEvent = new MarginReservedIntegrationEvent(
            domainEvent.EventId,
            domainEvent.MarginAccountId.ToString("D"),
            domainEvent.Amount,
            domainEvent.ReservedMargin,
            domainEvent.OccurredAtUtc);

        return CreateMessage(integrationEvent);
    }

    private static OutboxMessage CreateMarginReleasedMessage(MarginReleasedDomainEvent domainEvent)
    {
        var integrationEvent = new MarginReleasedIntegrationEvent(
            domainEvent.EventId,
            domainEvent.MarginAccountId.ToString("D"),
            domainEvent.Amount,
            domainEvent.ReservedMargin,
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