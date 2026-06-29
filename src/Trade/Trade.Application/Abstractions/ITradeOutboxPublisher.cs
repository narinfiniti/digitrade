namespace Trade.Application.Abstractions;

public interface ITradeOutboxPublisher
{
    Task PublishPendingAsync(CancellationToken cancellationToken = default);
}