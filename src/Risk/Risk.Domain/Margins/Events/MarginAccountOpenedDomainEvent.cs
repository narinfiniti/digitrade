using DigiTrade.SharedKernel.Events;

namespace Risk.Domain.Margins.Events;

public sealed record MarginAccountOpenedDomainEvent(
    Guid EventId,
    Guid MarginAccountId,
    string AccountId,
    string CurrencyCode,
    decimal TotalMargin,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;