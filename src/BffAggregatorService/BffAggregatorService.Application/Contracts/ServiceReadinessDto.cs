namespace BffAggregatorService.Application.Contracts;

public sealed record ServiceReadinessDto(
    bool IsHealthy,
    int TotalServices,
    int HealthyServicesCount,
    int UnhealthyServicesCount,
    IReadOnlyCollection<string> HealthyServices,
    IReadOnlyCollection<string> UnhealthyServices);
