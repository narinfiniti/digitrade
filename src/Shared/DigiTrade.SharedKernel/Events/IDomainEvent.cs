using MediatR;

namespace DigiTrade.SharedKernel.Events;

public interface IDomainEvent : INotification
{
    Guid EventId { get; }

    DateTimeOffset OccurredAtUtc { get; }
}