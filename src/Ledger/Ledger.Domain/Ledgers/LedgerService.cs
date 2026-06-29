namespace Ledger.Domain.Ledgers;

public sealed class LedgerService : ILedgerService
{
    public LedgerEntry Post(
        Guid settlementId,
        string accountId,
        string currencyCode,
        IReadOnlyCollection<LedgerPostingInstruction> postingLines,
        DateTimeOffset postedAtUtc)
    {
        if (settlementId == Guid.Empty)
        {
            throw new ArgumentException("Ledger settlement id is required.", nameof(settlementId));
        }

        if (string.IsNullOrWhiteSpace(accountId))
        {
            throw new ArgumentException("Ledger account id is required.", nameof(accountId));
        }

        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            throw new ArgumentException("Ledger currency code is required.", nameof(currencyCode));
        }

        ArgumentNullException.ThrowIfNull(postingLines);

        if (postingLines.Count < 2)
        {
            throw new ArgumentException("Ledger posting requires at least one debit and one credit line.", nameof(postingLines));
        }

        var normalizedPostingLines = postingLines
            .Select(postingLine => NormalizePostingLine(postingLine, nameof(postingLines)))
            .ToArray();

        var debitTotal = normalizedPostingLines
            .Where(postingLine => postingLine.Side == LedgerPostingSide.Debit)
            .Sum(postingLine => postingLine.Amount);

        var creditTotal = normalizedPostingLines
            .Where(postingLine => postingLine.Side == LedgerPostingSide.Credit)
            .Sum(postingLine => postingLine.Amount);

        if (debitTotal == 0m || creditTotal == 0m)
        {
            throw new ArgumentException("Ledger posting requires both debit and credit lines.", nameof(postingLines));
        }

        if (debitTotal != creditTotal)
        {
            throw new ArgumentException("Ledger posting must remain double-entry balanced.", nameof(postingLines));
        }

        var ledgerEntryId = Guid.NewGuid();
        var normalizedAccountId = accountId.Trim();
        var normalizedCurrencyCode = currencyCode.Trim().ToUpperInvariant();
        var postedDomainEvent = new LedgerEntryPostedDomainEvent(
            Guid.NewGuid(),
            ledgerEntryId,
            settlementId,
            normalizedAccountId,
            normalizedCurrencyCode,
            debitTotal,
            normalizedPostingLines.Length,
            postedAtUtc);

        return new LedgerEntry
        {
            Id = ledgerEntryId,
            SettlementId = settlementId,
            AccountId = normalizedAccountId,
            CurrencyCode = normalizedCurrencyCode,
            TotalAmount = debitTotal,
            PostedAtUtc = postedAtUtc,
            PostingLines = Array.AsReadOnly(normalizedPostingLines),
            Version = 1,
            CreatedAt = postedAtUtc,
            UpdatedAt = postedAtUtc,
            DomainEvents = [postedDomainEvent],
        };
    }

    private static LedgerPostingLine NormalizePostingLine(LedgerPostingInstruction postingLine, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(postingLine);

        if (string.IsNullOrWhiteSpace(postingLine.AccountCode))
        {
            throw new ArgumentException("Ledger posting line account code is required.", parameterName);
        }

        if (postingLine.Side is not LedgerPostingSide.Debit and not LedgerPostingSide.Credit)
        {
            throw new ArgumentException("Ledger posting line side must be either debit or credit.", parameterName);
        }

        if (postingLine.Amount <= 0m)
        {
            throw new ArgumentException("Ledger posting line amount must be positive.", parameterName);
        }

        return new LedgerPostingLine(
            Guid.NewGuid(),
            postingLine.AccountCode.Trim().ToUpperInvariant(),
            postingLine.Side,
            postingLine.Amount);
    }
}