using Trade.Domain.Trades.Events;

namespace Trade.Domain.Trades;

public sealed class TradeService : ITradeService
{

    public Trade Open(
        string accountId,
        string instrumentId,
        TradeDirection direction,
        decimal quantity,
        decimal openPrice,
        DateTimeOffset openedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            throw new ArgumentException("Trade account id is required.", nameof(accountId));
        }

        if (string.IsNullOrWhiteSpace(instrumentId))
        {
            throw new ArgumentException("Trade instrument id is required.", nameof(instrumentId));
        }

        if (quantity <= 0m)
        {
            throw new ArgumentException("Trade quantity must be greater than zero.", nameof(quantity));
        }

        if (openPrice <= 0m)
        {
            throw new ArgumentException("Trade open price must be greater than zero.", nameof(openPrice));
        }

        var tradeId = Guid.NewGuid();
        var openedEvent = new TradeOpenedDomainEvent(
            Guid.NewGuid(),
            tradeId,
            accountId.Trim(),
            instrumentId.Trim(),
            direction,
            quantity,
            openPrice,
            openedAtUtc);

        var trade = new Trade
        {
            Id = tradeId,
            AccountId = accountId.Trim(),
            InstrumentId = instrumentId.Trim(),
            Direction = direction,
            Status = TradeStatus.Open,
            Quantity = quantity,
            OpenPrice = openPrice,
            OpenedAtUtc = openedAtUtc,
            Version = 1,
            CreatedAt = openedAtUtc,
            UpdatedAt = openedAtUtc,
            DomainEvents = [openedEvent],
        };

        return trade;
    }

    public Trade Close(Trade trade, decimal closePrice, DateTimeOffset closedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(trade);

        if (closePrice <= 0m)
        {
            throw new ArgumentException("Trade close price must be greater than zero.", nameof(closePrice));
        }

        if (trade.Status == TradeStatus.Closed)
        {
            throw new InvalidOperationException("Trade is already closed.");
        }

        if (closedAtUtc < trade.OpenedAtUtc)
        {
            throw new ArgumentException("Trade cannot close before it was opened.", nameof(closedAtUtc));
        }

        trade.Status = TradeStatus.Closed;
        trade.ClosePrice = closePrice;
        trade.ClosedAtUtc = closedAtUtc;
        trade.UpdatedAt = closedAtUtc;
        trade.Version = trade.Version == int.MaxValue ? 1 : trade.Version + 1;
        trade.DomainEvents = trade.DomainEvents
            .Concat([
                new TradeClosedDomainEvent(
                    Guid.NewGuid(),
                    trade.Id,
                    closePrice,
                    closedAtUtc),
            ])
            .ToArray();

        return trade;
    }
}