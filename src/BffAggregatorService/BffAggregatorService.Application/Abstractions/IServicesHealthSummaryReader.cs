using BffAggregatorService.Application.Models;

namespace BffAggregatorService.Application.Abstractions;

public interface IServicesHealthSummaryReader
{
    Task<ServiceHealthSummaryModel> GetAsync(string correlationId, CancellationToken cancellationToken = default);
}