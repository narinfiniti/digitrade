using DigiTrade.SharedKernel.Events;

namespace Order.Application.Abstractions;

public interface IOrderOutboxWriter
{
    Task EnqueueAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}