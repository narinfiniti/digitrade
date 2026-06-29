using DigiTrade.SharedKernel.Abstractions;
using DigiTrade.SharedKernel.Events;

namespace Ledger.Domain.Ledgers;

public sealed class LedgerEntry : IAggregateRoot<Guid>
{
    public Guid Id { get; internal set; }

    public Guid SettlementId { get; internal set; }

    public string AccountId { get; internal set; } = string.Empty;

    public string CurrencyCode { get; internal set; } = string.Empty;

    public decimal TotalAmount { get; internal set; }

    public DateTimeOffset PostedAtUtc { get; internal set; }

    public IReadOnlyCollection<LedgerPostingLine> PostingLines { get; internal set; } = Array.Empty<LedgerPostingLine>();

    public int Version { get; internal set; }

    public DateTimeOffset CreatedAt { get; internal set; }

    public DateTimeOffset UpdatedAt { get; internal set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents { get; internal set; } = Array.Empty<IDomainEvent>();

    public void ClearDomainEvents()
    {
        DomainEvents = Array.Empty<IDomainEvent>();
    }
}

public sealed record LedgerPostingInstruction(
    string AccountCode,
    LedgerPostingSide Side,
    decimal Amount);

public sealed record LedgerPostingLine(
    Guid LineId,
    string AccountCode,
    LedgerPostingSide Side,
    decimal Amount);

public enum LedgerPostingSide
{
    Unspecified = 0,
    Debit = 1,
    Credit = 2,
}

public sealed record LedgerEntryPostedDomainEvent(
    Guid EventId,
    Guid LedgerEntryId,
    Guid SettlementId,
    string AccountId,
    string CurrencyCode,
    decimal TotalAmount,
    int PostingLineCount,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;