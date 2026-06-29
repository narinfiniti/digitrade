using BffOrchestratorService.Domain.Models;

namespace BffOrchestratorService.Domain.Abstractions;

public interface IBusinessProcessStateStore
{
    Task AddAsync(BusinessProcessStateModel processState, CancellationToken cancellationToken = default);

    Task UpdateAsync(BusinessProcessStateModel processState, CancellationToken cancellationToken = default);

    Task<BusinessProcessStateModel?> FindByProcessIdAsync(Guid processId, CancellationToken cancellationToken = default);

    Task<BusinessProcessStateModel?> FindByProcessNameAndIdempotencyKeyAsync(string processName, string idempotencyKey, CancellationToken cancellationToken = default);

    Task<BusinessProcessStateModel?> FindActiveByProcessKeyAsync(string processKey, DateTimeOffset asOfUtc, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
