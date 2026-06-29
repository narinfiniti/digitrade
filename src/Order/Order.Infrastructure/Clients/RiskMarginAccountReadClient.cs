using System.Net;
using System.Net.Http.Json;
using Order.Application.Abstractions;
using Order.Application.Models;

namespace Order.Infrastructure.Clients;

public sealed class RiskMarginAccountReadClient(HttpClient httpClient) : IMarginAccountReadPort
{
    public async Task<MarginAccountSnapshotModel?> GetByIdAsync(Guid marginAccountId, CancellationToken cancellationToken = default)
    {
        if (marginAccountId == Guid.Empty)
        {
            throw new ArgumentException("MarginAccountId must be provided.", nameof(marginAccountId));
        }

        EnsureBaseAddressConfigured();

        using var response = await httpClient.GetAsync($"/api/v1/margin-accounts/{marginAccountId}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<MarginAccountSnapshotModel>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Risk service returned an empty margin account payload.");
    }

    private void EnsureBaseAddressConfigured()
    {
        if (httpClient.BaseAddress is null)
        {
            throw new InvalidOperationException("Services:Risk:BaseUrl must be configured before calling the Risk margin account read port.");
        }
    }
}