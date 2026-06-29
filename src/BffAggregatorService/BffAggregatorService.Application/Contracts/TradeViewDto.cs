namespace BffAggregatorService.Application.Contracts;

public sealed record TradeViewDto(
    string TradeId,
    string AccountId,
    string InstrumentId,
    decimal Quantity,
    decimal Price,
    string Side,
    DateTimeOffset ExecutedAt);