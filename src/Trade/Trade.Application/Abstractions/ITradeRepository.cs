using DigiTrade.Persistence.Abstractions;
using TradeAggregate = Trade.Domain.Trades.Trade;

namespace Trade.Application.Abstractions;

public interface ITradeRepository : IUnitOfWork
{
    Task<TradeAggregate?> FindByIdAsync(Guid tradeId, CancellationToken cancellationToken = default);

    Task AddAsync(TradeAggregate trade, CancellationToken cancellationToken = default);
}