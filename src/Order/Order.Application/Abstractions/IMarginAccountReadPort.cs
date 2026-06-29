using Order.Application.Models;

namespace Order.Application.Abstractions;

public interface IMarginAccountReadPort
{
    Task<MarginAccountSnapshotModel?> GetByIdAsync(Guid marginAccountId, CancellationToken cancellationToken = default);
}