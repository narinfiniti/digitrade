using DigiTrade.SharedKernel.Events;

namespace DigiTrade.SharedKernel.Abstractions;

public interface IAggregateRoot
{
}

public interface IAggregateRoot<out TId> :
    IAggregateRoot,
    IEntity<TId>,
    IVersionedEntity,
    ITrackTimestamps,
    IHasDomainEvents
    where TId : notnull
{
}