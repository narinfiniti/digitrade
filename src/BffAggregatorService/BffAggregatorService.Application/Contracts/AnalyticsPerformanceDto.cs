namespace BffAggregatorService.Application.Contracts;

public sealed record AnalyticsPerformanceDto(
    string AccountId,
    decimal WinRate,
    decimal SharpeRatio,
    decimal AvgHoldingHours,
    IReadOnlyCollection<string> SourceServices,
    bool IsComplete);