using System.Net;
using BffAggregatorService.Application.Abstractions;
using BffAggregatorService.Application.Models;

namespace BffAggregatorService.Infrastructure.Clients;

public sealed class DownstreamServiceHealthClient(string serviceName, HttpClient httpClient) : IDownstreamServiceHealthClient
{
    private const string HealthPath = "/health/live";

    public async Task<DownstreamServiceHealthModel> GetHealthAsync(string correlationId, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, HealthPath);
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            request.Headers.TryAddWithoutValidation("X-Correlation-Id", correlationId);
        }

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            return new DownstreamServiceHealthModel(
                serviceName,
                response.IsSuccessStatusCode,
                (int)response.StatusCode,
                new Uri(httpClient.BaseAddress!, HealthPath).ToString(),
                response.IsSuccessStatusCode ? null : $"Downstream service returned HTTP {(int)response.StatusCode}.");
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            return new DownstreamServiceHealthModel(
                serviceName,
                false,
                (int)HttpStatusCode.ServiceUnavailable,
                new Uri(httpClient.BaseAddress!, HealthPath).ToString(),
                exception.Message);
        }
    }
}
