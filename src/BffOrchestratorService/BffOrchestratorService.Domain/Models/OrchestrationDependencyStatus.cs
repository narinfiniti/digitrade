namespace BffOrchestratorService.Domain.Models;

public sealed record OrchestrationDependencyStatusModel(
    string ServiceName,
    bool IsHealthy,
    int StatusCode,
    string Endpoint,
    string? FailureReason);