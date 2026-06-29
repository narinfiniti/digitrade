namespace BffAggregatorService.Application.Contracts;

public sealed record PortfolioQueryDto(
    string AccountId,
    decimal NetAssetValue,
    decimal UnrealizedPnl,
    decimal RealizedPnl,
    IReadOnlyCollection<string> SourceServices,
    bool IsComplete);