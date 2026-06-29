namespace Settlement.Application.Abstractions;

public interface ISettlementOutboxPublisher
{
    Task PublishPendingAsync(CancellationToken cancellationToken = default);
}