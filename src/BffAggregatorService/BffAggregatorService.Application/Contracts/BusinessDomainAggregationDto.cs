namespace BffAggregatorService.Application.Contracts;

public sealed record BusinessDomainAggregationDto(
    string Domain,
    string Description,
    bool IsHealthy,
    IReadOnlyCollection<string> RequiredServices,
    IReadOnlyCollection<string> HealthyServices,
    IReadOnlyCollection<string> UnhealthyServices);
