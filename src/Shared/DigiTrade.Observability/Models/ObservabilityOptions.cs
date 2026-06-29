namespace DigiTrade.Observability.Models;

public sealed class ObservabilityOptions
{
    public string ServiceName { get; init; } = string.Empty;

    public string ServiceVersion { get; init; } = "1.0.0";

    public string? OtlpEndpoint { get; init; }

    public bool EnableConsoleExporter { get; init; }
}