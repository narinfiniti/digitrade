using BffAggregatorService.Application.Abstractions;
using BffAggregatorService.Application.Models;

namespace BffAggregatorService.Infrastructure.Services;

public sealed class ServicesHealthSummaryReader(
    IEnumerable<IDownstreamServiceHealthClient> downstreamServiceHealthClients)
        : IServicesHealthSummaryReader
{
    public async Task<ServiceHealthSummaryModel> GetAsync(string correlationId, CancellationToken cancellationToken = default)
    {
        var serviceStatuses = await Task.WhenAll(
            downstreamServiceHealthClients.Select(client => client.GetHealthAsync(correlationId, cancellationToken)));

        Array.Sort(serviceStatuses, static (left, right) => string.Compare(left.ServiceName, right.ServiceName, StringComparison.Ordinal));
        return new ServiceHealthSummaryModel(serviceStatuses.All(service => service.IsHealthy), serviceStatuses);
    }
}