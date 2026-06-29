namespace Order.Domain.Orders;

public interface IOrderService
{
    Order Place(
        string accountId,
        string instrumentId,
        OrderDirection direction,
        decimal quantity,
        decimal requestedPrice,
        DateTimeOffset submittedAtUtc);

    Order Accept(Order order, DateTimeOffset acceptedAtUtc);

    Order Reject(Order order, DateTimeOffset rejectedAtUtc);

    Order Cancel(Order order, DateTimeOffset cancelledAtUtc);
}