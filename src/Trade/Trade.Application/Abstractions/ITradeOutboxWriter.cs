using DigiTrade.SharedKernel.Events;

namespace Trade.Application.Abstractions;

public interface ITradeOutboxWriter
{
    Task EnqueueAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}