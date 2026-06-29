using DigiTrade.SharedKernel.Events;

namespace Risk.Application.Abstractions;

public interface IRiskOutboxWriter
{
    Task EnqueueAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}