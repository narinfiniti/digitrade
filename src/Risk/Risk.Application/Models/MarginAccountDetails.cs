namespace Risk.Application.Models;

public sealed record MarginAccountDetailsModel(
    Guid MarginAccountId,
    string AccountId,
    string CurrencyCode,
    decimal TotalMargin,
    decimal ReservedMargin,
    int Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);