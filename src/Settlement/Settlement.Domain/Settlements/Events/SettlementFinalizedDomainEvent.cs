using DigiTrade.SharedKernel.Events;

namespace Settlement.Domain.Settlements.Events;

public sealed record SettlementFinalizedDomainEvent(
    Guid EventId,
    Guid SettlementId,
    Guid TradeId,
    string AccountId,
    string CurrencyCode,
    decimal NetAmount,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;