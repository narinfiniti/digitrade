namespace BffAggregatorService.Application.Contracts;

public sealed record PositionItemDto(
    string PositionId,
    string InstrumentId,
    decimal Quantity,
    decimal AveragePrice,
    decimal MarkPrice,
    decimal UnrealizedPnl);