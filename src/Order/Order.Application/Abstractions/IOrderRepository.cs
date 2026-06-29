using DigiTrade.Persistence.Abstractions;
using OrderAggregate = Order.Domain.Orders.Order;

namespace Order.Application.Abstractions;

public interface IOrderRepository : IUnitOfWork
{
    Task<OrderAggregate?> FindByIdAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task AddAsync(OrderAggregate order, CancellationToken cancellationToken = default);
}