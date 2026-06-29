using System.ComponentModel.DataAnnotations;

namespace BffOrchestratorService.Application.Contracts;

public sealed record CreateOrchestrationShellInput(
  [Required(ErrorMessage = "'Flow Name' must not be empty.")] string FlowName);