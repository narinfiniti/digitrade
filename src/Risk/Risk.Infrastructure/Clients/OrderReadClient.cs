using System.Net;
using System.Net.Http.Json;
using Risk.Application.Abstractions;
using Risk.Application.Models;

namespace Risk.Infrastructure.Clients;

public sealed class OrderReadClient(HttpClient httpClient) : IOrderReadPort
{
    public async Task<OrderSnapshotModel?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        if (orderId == Guid.Empty)
        {
            throw new ArgumentException("OrderId must be provided.", nameof(orderId));
        }

        EnsureBaseAddressConfigured();

        using var response = await httpClient.GetAsync($"/api/v1/orders/{orderId}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<OrderSnapshotModel>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Order service returned an empty order payload.");
    }

    private void EnsureBaseAddressConfigured()
    {
        if (httpClient.BaseAddress is null)
        {
            throw new InvalidOperationException("Services:Order:BaseUrl must be configured before calling the Order read port.");
        }
    }
}