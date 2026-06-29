using DigiTrade.SharedKernel.Events;

namespace Settlement.Domain.Settlements.Events;

public sealed record SettlementFailedDomainEvent(
    Guid EventId,
    Guid SettlementId,
    Guid TradeId,
    string FailureReason,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;