namespace BffOrchestratorService.Application.Contracts;

public sealed record BusinessProcessDefinitionDto(
    string ProcessCode,
    string Mode,
    string FlowName,
    IReadOnlyCollection<string> InvolvedServices,
    string Description);
