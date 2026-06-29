using BffAggregatorService.Application.Models;

namespace BffAggregatorService.Application.Abstractions;

public interface IDownstreamServiceHealthClient
{
    Task<DownstreamServiceHealthModel> GetHealthAsync(string correlationId, CancellationToken cancellationToken = default);
}