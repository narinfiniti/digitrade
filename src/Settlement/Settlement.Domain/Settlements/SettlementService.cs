using DigiTrade.SharedKernel.Events;
using Settlement.Domain.Settlements.Events;

namespace Settlement.Domain.Settlements;

public sealed class SettlementService : ISettlementService
{
    public Settlement Initiate(
        Guid tradeId,
        string accountId,
        string currencyCode,
        decimal netAmount,
        DateTimeOffset initiatedAtUtc)
    {
        if (tradeId == Guid.Empty)
        {
            throw new ArgumentException("Settlement trade id is required.", nameof(tradeId));
        }

        if (string.IsNullOrWhiteSpace(accountId))
        {
            throw new ArgumentException("Settlement account id is required.", nameof(accountId));
        }

        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            throw new ArgumentException("Settlement currency code is required.", nameof(currencyCode));
        }

        if (netAmount == 0m)
        {
            throw new ArgumentException("Settlement net amount must be non-zero.", nameof(netAmount));
        }

        var settlementId = Guid.NewGuid();
        var normalizedAccountId = accountId.Trim();
        var normalizedCurrencyCode = currencyCode.Trim().ToUpperInvariant();
        var initiatedEvent = new SettlementInitiatedDomainEvent(
            Guid.NewGuid(),
            settlementId,
            tradeId,
            normalizedAccountId,
            normalizedCurrencyCode,
            netAmount,
            initiatedAtUtc);

        return new Settlement
        {
            Id = settlementId,
            TradeId = tradeId,
            AccountId = normalizedAccountId,
            CurrencyCode = normalizedCurrencyCode,
            NetAmount = netAmount,
            Status = SettlementStatus.PendingFinalization,
            InitiatedAtUtc = initiatedAtUtc,
            Version = 1,
            CreatedAt = initiatedAtUtc,
            UpdatedAt = initiatedAtUtc,
            DomainEvents = [initiatedEvent],
        };
    }

    public Settlement Finalize(Settlement settlement, DateTimeOffset finalizedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(settlement);
        EnsurePendingFinalization(settlement, "Only pending settlements can be finalized.");
        EnsureMutationTimestamp(settlement, finalizedAtUtc, nameof(finalizedAtUtc));

        settlement.Status = SettlementStatus.Finalized;
        settlement.FinalizedAtUtc = finalizedAtUtc;
        settlement.UpdatedAt = finalizedAtUtc;
        settlement.Version = GetNextVersion(settlement.Version);
        AppendDomainEvent(
            settlement,
            new SettlementFinalizedDomainEvent(
                Guid.NewGuid(),
                settlement.Id,
                settlement.TradeId,
                settlement.AccountId,
                settlement.CurrencyCode,
                settlement.NetAmount,
                finalizedAtUtc));

        return settlement;
    }

    public Settlement Fail(Settlement settlement, string failureReason, DateTimeOffset failedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(settlement);

        if (string.IsNullOrWhiteSpace(failureReason))
        {
            throw new ArgumentException("Settlement failure reason is required.", nameof(failureReason));
        }

        EnsurePendingFinalization(settlement, "Only pending settlements can be failed.");
        EnsureMutationTimestamp(settlement, failedAtUtc, nameof(failedAtUtc));

        settlement.Status = SettlementStatus.Failed;
        settlement.FailedAtUtc = failedAtUtc;
        settlement.FailureReason = failureReason.Trim();
        settlement.UpdatedAt = failedAtUtc;
        settlement.Version = GetNextVersion(settlement.Version);
        AppendDomainEvent(
            settlement,
            new SettlementFailedDomainEvent(
                Guid.NewGuid(),
                settlement.Id,
                settlement.TradeId,
                settlement.FailureReason,
                failedAtUtc));

        return settlement;
    }

    private static void EnsurePendingFinalization(Settlement settlement, string errorMessage)
    {
        if (settlement.Status != SettlementStatus.PendingFinalization)
        {
            throw new InvalidOperationException(errorMessage);
        }
    }

    private static void EnsureMutationTimestamp(Settlement settlement, DateTimeOffset occurredAtUtc, string parameterName)
    {
        if (occurredAtUtc < settlement.UpdatedAt)
        {
            throw new ArgumentException("Settlement mutation timestamp cannot move backwards.", parameterName);
        }
    }

    private static void AppendDomainEvent(Settlement settlement, IDomainEvent domainEvent)
    {
        settlement.DomainEvents = settlement.DomainEvents
            .Concat([domainEvent])
            .ToArray();
    }

    private static int GetNextVersion(int currentVersion)
    {
        return currentVersion == int.MaxValue ? 1 : currentVersion + 1;
    }
}