using DigiTrade.Persistence.Abstractions;
using MarginAccountAggregate = Risk.Domain.Margins.MarginAccount;

namespace Risk.Application.Abstractions;

public interface IMarginAccountRepository : IUnitOfWork
{
    Task<MarginAccountAggregate?> FindByIdAsync(Guid marginAccountId, CancellationToken cancellationToken = default);

    Task AddAsync(MarginAccountAggregate marginAccount, CancellationToken cancellationToken = default);
}