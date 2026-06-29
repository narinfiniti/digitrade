namespace Risk.Application.Abstractions;

public interface IRiskOutboxPublisher
{
    Task PublishPendingAsync(CancellationToken cancellationToken = default);
}