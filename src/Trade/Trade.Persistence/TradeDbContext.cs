using Microsoft.EntityFrameworkCore;
using MediatR;
using DigiTrade.Persistence.Extensions;
using Trade.Persistence.Outbox;
using Trade.Persistence.Outbox.Configurations;
using Trade.Persistence.Trades.Configurations;
using TradeAggregate = Trade.Domain.Trades.Trade;

namespace Trade.Persistence;

public sealed class TradeDbContext(DbContextOptions<TradeDbContext> options, IMediator? mediator = null) : DbContext(options)
{
    private readonly IMediator? mediator = mediator;

    public const string DefaultSchema = "trade";

    public DbSet<TradeAggregate> Trades => Set<TradeAggregate>();

    public DbSet<TradeOutboxMessageEntity> OutboxMessages => Set<TradeOutboxMessageEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DefaultSchema);
        modelBuilder.ApplyConfiguration(new TradeEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new TradeOutboxMessageEntityTypeConfiguration());

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