using DigiTrade.Persistence.Abstractions;
using SettlementAggregate = Settlement.Domain.Settlements.Settlement;

namespace Settlement.Application.Abstractions;

public interface ISettlementRepository : IUnitOfWork
{
    Task<SettlementAggregate?> FindByIdAsync(Guid settlementId, CancellationToken cancellationToken = default);

    Task AddAsync(SettlementAggregate settlement, CancellationToken cancellationToken = default);
}