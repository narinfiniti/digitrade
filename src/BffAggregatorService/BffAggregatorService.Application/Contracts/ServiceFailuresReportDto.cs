namespace BffAggregatorService.Application.Contracts;

public sealed record ServiceFailuresReportDto(
    bool IsHealthy,
    int FailureCount,
    IReadOnlyCollection<ServiceFailureDto> Failures);
