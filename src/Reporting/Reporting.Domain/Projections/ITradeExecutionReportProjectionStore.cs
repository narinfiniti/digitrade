namespace Reporting.Domain.Projections;

public interface ITradeExecutionReportProjectionStore
{
    Task<TradeExecutionReportProjection?> GetByAggregateIdAsync(string aggregateId, CancellationToken cancellationToken = default);

    Task UpsertAsync(TradeExecutionReportProjection projection, CancellationToken cancellationToken = default);
}