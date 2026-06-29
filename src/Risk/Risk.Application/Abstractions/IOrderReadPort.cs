using Risk.Application.Models;

namespace Risk.Application.Abstractions;

public interface IOrderReadPort
{
    Task<OrderSnapshotModel?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default);
}