namespace Order.Application.Models;

public sealed record MarginAccountSnapshotModel(
    Guid MarginAccountId,
    string AccountId,
    string CurrencyCode,
    decimal TotalMargin,
    decimal ReservedMargin,
    int Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);