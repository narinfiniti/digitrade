namespace Settlement.Domain.Settlements;

public interface ISettlementService
{
    Settlement Initiate(
        Guid tradeId,
        string accountId,
        string currencyCode,
        decimal netAmount,
        DateTimeOffset initiatedAtUtc);

    Settlement Finalize(Settlement settlement, DateTimeOffset finalizedAtUtc);

    Settlement Fail(Settlement settlement, string failureReason, DateTimeOffset failedAtUtc);
}