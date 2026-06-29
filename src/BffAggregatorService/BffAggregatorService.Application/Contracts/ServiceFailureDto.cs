namespace BffAggregatorService.Application.Contracts;

public sealed record ServiceFailureDto(
    string ServiceName,
    int StatusCode,
    string Endpoint,
    string? FailureReason);
