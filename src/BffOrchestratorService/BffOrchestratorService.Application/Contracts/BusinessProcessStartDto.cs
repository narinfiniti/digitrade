namespace BffOrchestratorService.Application.Contracts;

public sealed record BusinessProcessStartDto(
    string ProcessCode,
    string Mode,
    string FlowName,
    IReadOnlyCollection<string> InvolvedServices,
    OrchestrationShellDto Orchestration);
