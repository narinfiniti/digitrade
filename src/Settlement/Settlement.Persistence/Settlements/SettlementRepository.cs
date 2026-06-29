using Microsoft.EntityFrameworkCore;
using Settlement.Application.Abstractions;
using SettlementAggregate = Settlement.Domain.Settlements.Settlement;

namespace Settlement.Persistence.Settlements;

public sealed class SettlementRepository(SettlementDbContext dbContext) : ISettlementRepository
{
    public async Task AddAsync(SettlementAggregate settlement, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settlement);

        await dbContext.Settlements.AddAsync(settlement, cancellationToken);
    }

    public Task<SettlementAggregate?> FindByIdAsync(Guid settlementId, CancellationToken cancellationToken = default)
    {
        return dbContext.Settlements.SingleOrDefaultAsync(settlement => settlement.Id == settlementId, cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return SettlementConcurrencyRetryPolicy.ExecuteAsync(
            ct => dbContext.SaveChangesAsync(ct),
            cancellationToken);
    }

    public Task<int> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        return SettlementConcurrencyRetryPolicy.ExecuteAsync(
            ct => dbContext.SaveEntitiesAsync(ct),
            cancellationToken);
    }
}