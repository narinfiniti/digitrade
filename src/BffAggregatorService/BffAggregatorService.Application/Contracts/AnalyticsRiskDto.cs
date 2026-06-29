namespace BffAggregatorService.Application.Contracts;

public sealed record AnalyticsRiskDto(
    string AccountId,
    decimal ValueAtRisk,
    decimal StressLossEstimate,
    decimal MarginUtilization,
    IReadOnlyCollection<string> SourceServices,
    bool IsComplete);