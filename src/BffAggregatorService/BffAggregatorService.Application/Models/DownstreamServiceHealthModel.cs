namespace BffAggregatorService.Application.Models;

public sealed record DownstreamServiceHealthModel(
    string ServiceName,
    bool IsHealthy,
    int StatusCode,
    string Endpoint,
    string? FailureReason);