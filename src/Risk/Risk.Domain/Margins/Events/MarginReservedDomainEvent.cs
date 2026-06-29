using DigiTrade.SharedKernel.Events;

namespace Risk.Domain.Margins.Events;

public sealed record MarginReservedDomainEvent(
    Guid EventId,
    Guid MarginAccountId,
    decimal Amount,
    decimal ReservedMargin,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;