using DigiTrade.Persistence.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Order.Persistence.Outbox;
using Order.Persistence.Outbox.Configurations;
using Order.Persistence.Orders.Configurations;
using OrderAggregate = Order.Domain.Orders.Order;

namespace Order.Persistence;

public sealed class OrderDbContext(DbContextOptions<OrderDbContext> options, IMediator? mediator = null) : DbContext(options)
{
    private readonly IMediator? mediator = mediator;

    public const string DefaultSchema = "order";

    public DbSet<OrderAggregate> Orders => Set<OrderAggregate>();

    public DbSet<OrderOutboxMessageEntity> OutboxMessages => Set<OrderOutboxMessageEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DefaultSchema);
        modelBuilder.ApplyConfiguration(new OrderEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new OrderOutboxMessageEntityTypeConfiguration());

        base.OnModelCreating(modelBuilder);
    }

    public async Task<int> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        if (mediator is not null)
        {
            await mediator.DispatchDomainEventsAsync(this, cancellationToken);
        }

        return await SaveChangesAsync(cancellationToken);
    }
}