using DigiTrade.SharedKernel.Events;

namespace Settlement.Application.Abstractions;

public interface ISettlementOutboxWriter
{
    Task EnqueueAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}