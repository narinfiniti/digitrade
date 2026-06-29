using System.Collections.Concurrent;
using Reporting.Domain.Projections;

namespace Reporting.Persistence.Stores;

public sealed class InMemoryTradeExecutionReportProjectionStore : ITradeExecutionReportProjectionStore
{
    private readonly ConcurrentDictionary<string, TradeExecutionReportProjection> projectionsByAggregateId =
        new(StringComparer.Ordinal);

    public Task<TradeExecutionReportProjection?> GetByAggregateIdAsync(string aggregateId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateId);

        projectionsByAggregateId.TryGetValue(aggregateId, out var projection);
        return Task.FromResult(projection);
    }

    public Task UpsertAsync(TradeExecutionReportProjection projection, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(projection);

        projectionsByAggregateId[projection.AggregateId] = projection;
        return Task.CompletedTask;
    }
}