namespace BffAggregatorService.Application.Contracts;

public sealed record TradesQueryDto(
    IReadOnlyCollection<TradeViewDto> Trades,
    IReadOnlyCollection<string> SourceServices,
    bool IsComplete);