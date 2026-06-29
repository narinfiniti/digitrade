using Microsoft.EntityFrameworkCore;
using Order.Application.Abstractions;
using OrderAggregate = Order.Domain.Orders.Order;

namespace Order.Persistence.Orders;

public sealed class OrderRepository(OrderDbContext dbContext) : IOrderRepository
{
    public async Task AddAsync(OrderAggregate order, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);

        await dbContext.Orders.AddAsync(order, cancellationToken);
    }

    public Task<OrderAggregate?> FindByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return dbContext.Orders.SingleOrDefaultAsync(order => order.Id == orderId, cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return OrderConcurrencyRetryPolicy.ExecuteAsync(
            ct => dbContext.SaveChangesAsync(ct),
            cancellationToken);
    }

    public Task<int> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        return OrderConcurrencyRetryPolicy.ExecuteAsync(
            ct => dbContext.SaveEntitiesAsync(ct),
            cancellationToken);
    }
}