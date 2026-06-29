using Ledger.Domain.Ledgers;

namespace Ledger.Application.Abstractions;

public interface ILedgerEntryRepository
{
    Task AddAsync(LedgerEntry ledgerEntry, CancellationToken cancellationToken = default);

    Task<LedgerEntry?> FindBySettlementIdAsync(Guid settlementId, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<int> SaveEntitiesAsync(CancellationToken cancellationToken = default);
}