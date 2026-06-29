using Microsoft.EntityFrameworkCore;
using Risk.Application.Abstractions;
using MarginAccountAggregate = Risk.Domain.Margins.MarginAccount;

namespace Risk.Persistence.Margins;

public sealed class MarginAccountRepository(RiskDbContext dbContext) : IMarginAccountRepository
{
    public async Task AddAsync(MarginAccountAggregate marginAccount, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(marginAccount);

        await dbContext.MarginAccounts.AddAsync(marginAccount, cancellationToken);
    }

    public Task<MarginAccountAggregate?> FindByIdAsync(Guid marginAccountId, CancellationToken cancellationToken = default)
    {
        return dbContext.MarginAccounts.SingleOrDefaultAsync(marginAccount => marginAccount.Id == marginAccountId, cancellationToken);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return RiskConcurrencyRetryPolicy.ExecuteAsync(
            ct => dbContext.SaveChangesAsync(ct),
            cancellationToken);
    }

    public Task<int> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        return RiskConcurrencyRetryPolicy.ExecuteAsync(
            ct => dbContext.SaveEntitiesAsync(ct),
            cancellationToken);
    }
}