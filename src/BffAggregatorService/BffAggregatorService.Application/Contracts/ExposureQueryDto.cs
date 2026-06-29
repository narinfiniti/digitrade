namespace BffAggregatorService.Application.Contracts;

public sealed record ExposureQueryDto(
    string AccountId,
    decimal GrossExposure,
    decimal NetExposure,
    decimal MarginUsage,
    IReadOnlyCollection<string> SourceServices,
    bool IsComplete);