namespace BffAggregatorService.Application.Models;

public sealed record ServiceHealthSummaryModel(
    bool IsHealthy,
    IReadOnlyCollection<DownstreamServiceHealthModel> Services);