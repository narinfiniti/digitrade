namespace Ledger.Application.Abstractions;

public interface ILedgerOutboxPublisher
{
    Task PublishPendingAsync(CancellationToken cancellationToken = default);
}