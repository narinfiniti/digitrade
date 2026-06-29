namespace Ledger.Domain.Ledgers;

public interface ILedgerService
{
    LedgerEntry Post(
        Guid settlementId,
        string accountId,
        string currencyCode,
        IReadOnlyCollection<LedgerPostingInstruction> postingLines,
        DateTimeOffset postedAtUtc);
}