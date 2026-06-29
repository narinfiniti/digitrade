namespace DigiTrade.Persistence.Abstractions;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<int> SaveEntitiesAsync(CancellationToken cancellationToken = default);
}