namespace BffOrchestratorService.Application.Contracts;

public sealed record StartBusinessProcessInput(string? BusinessKey, IReadOnlyDictionary<string, string>? Attributes);
