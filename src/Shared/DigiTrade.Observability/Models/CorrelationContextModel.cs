namespace DigiTrade.Observability.Models;

public sealed record CorrelationContextModel(string CorrelationId, string? CausationId = null);