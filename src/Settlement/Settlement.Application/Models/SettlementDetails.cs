using Settlement.Domain.Settlements;

namespace Settlement.Application.Models;

public sealed record SettlementDetailsModel(
    Guid SettlementId,
    Guid TradeId,
    string AccountId,
    string CurrencyCode,
    decimal NetAmount,
    SettlementStatus Status,
    DateTimeOffset InitiatedAtUtc,
    DateTimeOffset? FinalizedAtUtc,
    DateTimeOffset? FailedAtUtc,
    string? FailureReason,
    int Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);