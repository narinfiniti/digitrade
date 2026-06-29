using Microsoft.EntityFrameworkCore;
using Trade.Application.Abstractions;
using TradeAggregate = Trade.Domain.Trades.Trade;

namespace Trade.Persistence.Trades;

public sealed class TradeRepository(TradeDbContext dbContext) : ITradeRepository
{
    public async Task AddAsync(TradeAggregate trade, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(trade);

        await dbContext.Trades.AddAsync(trade, cancellationToken);
    }

    public Task<TradeAggregate?> FindByIdAsync(Guid tradeId, CancellationToken cancellationToken = default)
    {
        return dbContext.Trades
            .SingleOrDefaultAsync(trade => trade.Id == tradeId, cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return TradeConcurrencyRetryPolicy.ExecuteAsync(
            ct => dbContext.SaveChangesAsync(ct),
            cancellationToken);
    }

    public Task<int> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        return TradeConcurrencyRetryPolicy.ExecuteAsync(
            ct => dbContext.SaveEntitiesAsync(ct),
            cancellationToken);
    }
}