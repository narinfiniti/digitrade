namespace DigiTrade.Persistence.Abstractions;

public interface IPersistenceTransactionFactory
{
    Task<IPersistenceTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}