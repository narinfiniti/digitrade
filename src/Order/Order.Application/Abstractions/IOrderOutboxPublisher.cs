namespace Order.Application.Abstractions;

public interface IOrderOutboxPublisher
{
    Task PublishPendingAsync(CancellationToken cancellationToken = default);
}