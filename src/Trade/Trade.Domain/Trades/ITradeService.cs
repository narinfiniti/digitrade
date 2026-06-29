namespace Trade.Domain.Trades;

public interface ITradeService
{
    Trade Open(
        string accountId,
        string instrumentId,
        TradeDirection direction,
        decimal quantity,
        decimal openPrice,
        DateTimeOffset openedAtUtc);

    Trade Close(Trade trade, decimal closePrice, DateTimeOffset closedAtUtc);
}