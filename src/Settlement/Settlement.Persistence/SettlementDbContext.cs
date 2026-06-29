using DigiTrade.Persistence.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Settlement.Persistence.Outbox;
using Settlement.Persistence.Outbox.Configurations;
using Settlement.Persistence.Settlements.Configurations;
using SettlementAggregate = Settlement.Domain.Settlements.Settlement;

namespace Settlement.Persistence;

public sealed class SettlementDbContext(DbContextOptions<SettlementDbContext> options, IMediator? mediator = null) : DbContext(options)
{
    private readonly IMediator? mediator = mediator;

    public const string DefaultSchema = "settlement";

    public DbSet<SettlementAggregate> Settlements => Set<SettlementAggregate>();

    public DbSet<SettlementOutboxMessageEntity> OutboxMessages => Set<SettlementOutboxMessageEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DefaultSchema);
        modelBuilder.ApplyConfiguration(new SettlementEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new SettlementOutboxMessageEntityTypeConfiguration());

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