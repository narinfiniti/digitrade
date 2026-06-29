using DigiTrade.Messaging.Contracts;
using DigiTrade.Messaging.Persistence.Outbox;
using Ledger.Application.Abstractions;
using Ledger.Domain.Ledgers;
using Settlement.Application.Events;
using System.Text.Json;

namespace Ledger.Infrastructure.Consumers;

public sealed class SettlementFinalizedConsumer(
    ILedgerEntryRepository ledgerEntryRepository,
    IOutboxStore outboxStore,
    ILedgerService ledgerService) : IIntegrationEventConsumer<SettlementFinalizedIntegrationEvent>
{
    private const string CustomerSettlementBalanceAccountCode = "CUSTOMER_SETTLEMENT_BALANCE";
    private const string SettlementClearingAccountCode = "SETTLEMENT_CLEARING";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task ConsumeAsync(SettlementFinalizedIntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        if (!Guid.TryParse(integrationEvent.AggregateId, out var settlementId))
        {
            throw new ArgumentException("Settlement finalized event aggregate id must be a valid settlement id.", nameof(integrationEvent));
        }

        var existingLedgerEntry = await ledgerEntryRepository.FindBySettlementIdAsync(settlementId, cancellationToken);
        if (existingLedgerEntry is not null)
        {
            return;
        }

        var ledgerEntry = ledgerService.Post(
            settlementId,
            integrationEvent.AccountId,
            integrationEvent.CurrencyCode,
            CreatePostingLines(integrationEvent.NetAmount),
            integrationEvent.OccurredAtUtc);

        await ledgerEntryRepository.AddAsync(ledgerEntry, cancellationToken);
        await outboxStore.EnqueueAsync(CreateLedgerEntryPostedMessage(ledgerEntry), cancellationToken);
        await ledgerEntryRepository.SaveEntitiesAsync(cancellationToken);
    }

    private static IReadOnlyCollection<LedgerPostingInstruction> CreatePostingLines(decimal netAmount)
    {
        if (netAmount == 0m)
        {
            throw new ArgumentException("Settlement finalized event net amount must be non-zero.", nameof(netAmount));
        }

        var absoluteAmount = Math.Abs(netAmount);

        return netAmount > 0m
            ?
            [
                new LedgerPostingInstruction(CustomerSettlementBalanceAccountCode, LedgerPostingSide.Debit, absoluteAmount),
                new LedgerPostingInstruction(SettlementClearingAccountCode, LedgerPostingSide.Credit, absoluteAmount),
            ]
            :
            [
                new LedgerPostingInstruction(SettlementClearingAccountCode, LedgerPostingSide.Debit, absoluteAmount),
                new LedgerPostingInstruction(CustomerSettlementBalanceAccountCode, LedgerPostingSide.Credit, absoluteAmount),
            ];
    }

    private static OutboxMessage CreateLedgerEntryPostedMessage(LedgerEntry ledgerEntry)
    {
        var integrationEvent = new LedgerEntryPostedIntegrationEvent(
            Guid.NewGuid(),
            ledgerEntry.Id.ToString("D"),
            ledgerEntry.SettlementId.ToString("D"),
            ledgerEntry.AccountId,
            ledgerEntry.CurrencyCode,
            ledgerEntry.TotalAmount,
            ledgerEntry.PostedAtUtc);

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

    private sealed record LedgerEntryPostedIntegrationEvent(
        Guid EventId,
        string AggregateId,
        string SettlementId,
        string AccountId,
        string CurrencyCode,
        decimal TotalAmount,
        DateTimeOffset OccurredAtUtc) : IIntegrationEvent
    {
        public const string IntegrationEventName = "ledger.entry.posted";

        public string EventName => IntegrationEventName;

        public int EventVersion => 1;
    }
}