using DigiTrade.Persistence.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MarginAccountAggregate = Risk.Domain.Margins.MarginAccount;
using Risk.Persistence.Outbox;
using Risk.Persistence.Outbox.Configurations;
using Risk.Persistence.Margins.Configurations;

namespace Risk.Persistence;

public sealed class RiskDbContext(DbContextOptions<RiskDbContext> options, IMediator? mediator = null) : DbContext(options)
{
    private readonly IMediator? mediator = mediator;

    public const string DefaultSchema = "risk";

    public DbSet<MarginAccountAggregate> MarginAccounts => Set<MarginAccountAggregate>();

    public DbSet<RiskOutboxMessageEntity> OutboxMessages => Set<RiskOutboxMessageEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DefaultSchema);
        modelBuilder.ApplyConfiguration(new MarginAccountEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new RiskOutboxMessageEntityTypeConfiguration());

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