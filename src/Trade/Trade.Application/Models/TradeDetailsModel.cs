using Trade.Domain.Trades;

namespace Trade.Application.Models;

public sealed record TradeDetailsModel(
    Guid TradeId,
    string AccountId,
    string InstrumentId,
    TradeDirection Direction,
    TradeStatus Status,
    decimal Quantity,
    decimal OpenPrice,
    DateTimeOffset OpenedAtUtc,
    decimal? ClosePrice,
    DateTimeOffset? ClosedAtUtc,
    int Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);