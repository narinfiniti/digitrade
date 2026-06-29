using BffAggregatorService.Application.Models;

namespace BffAggregatorService.Application.UseCases;

internal static class QueryCompleteness
{
    public static bool IsComplete(ServiceHealthSummaryModel summary, IReadOnlyCollection<string> requiredServices)
    {
        var map = summary.Services.ToDictionary(service => service.ServiceName, service => service.IsHealthy, StringComparer.Ordinal);
        return requiredServices.All(service => map.TryGetValue(service, out var healthy) && healthy);
    }
}