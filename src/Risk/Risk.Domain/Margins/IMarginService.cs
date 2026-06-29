namespace Risk.Domain.Margins;

public interface IMarginService
{
    MarginAccount Open(
        string accountId,
        string currencyCode,
        decimal totalMargin,
        DateTimeOffset openedAtUtc);

    MarginAccount Reserve(
        MarginAccount marginAccount,
        decimal amount,
        DateTimeOffset reservedAtUtc);

    MarginAccount Release(
        MarginAccount marginAccount,
        decimal amount,
        DateTimeOffset releasedAtUtc);
}