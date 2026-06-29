using DigiTrade.SharedKernel.Events;

namespace Risk.Domain.Margins.Events;

public sealed record MarginReleasedDomainEvent(
    Guid EventId,
    Guid MarginAccountId,
    decimal Amount,
    decimal ReservedMargin,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;