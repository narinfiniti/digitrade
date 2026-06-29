using System.Net;
using BffOrchestratorService.Domain.Models;
using BffOrchestratorService.Infrastructure.Abstractions;

namespace BffOrchestratorService.Infrastructure.Clients;

public sealed class DownstreamServiceProbeClient(string serviceName, HttpClient httpClient) : IDownstreamOrchestrationProbeClient
{
    private const string HealthPath = "/health/live";

    public string ServiceName { get; } = serviceName;

    public async Task<OrchestrationDependencyStatusModel> GetStatusAsync(string correlationId, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, HealthPath);
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            request.Headers.TryAddWithoutValidation("X-Correlation-Id", correlationId);
        }

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            return new OrchestrationDependencyStatusModel(
                ServiceName,
                response.IsSuccessStatusCode,
                (int)response.StatusCode,
                new Uri(httpClient.BaseAddress!, HealthPath).ToString(),
                response.IsSuccessStatusCode ? null : $"Downstream service returned HTTP {(int)response.StatusCode}.");
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            return new OrchestrationDependencyStatusModel(
                ServiceName,
                false,
                (int)HttpStatusCode.ServiceUnavailable,
                new Uri(httpClient.BaseAddress!, HealthPath).ToString(),
                exception.Message);
        }
    }
}
