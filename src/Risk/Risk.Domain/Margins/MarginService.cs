using DigiTrade.SharedKernel.Events;
using Risk.Domain.Margins.Events;

namespace Risk.Domain.Margins;

public sealed class MarginService : IMarginService
{
    public MarginAccount Open(
        string accountId,
        string currencyCode,
        decimal totalMargin,
        DateTimeOffset openedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            throw new ArgumentException("Margin account id is required.", nameof(accountId));
        }

        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            throw new ArgumentException("Margin currency code is required.", nameof(currencyCode));
        }

        if (totalMargin < 0m)
        {
            throw new ArgumentException("Margin total cannot be negative.", nameof(totalMargin));
        }

        var marginAccountId = Guid.NewGuid();
        var openedEvent = new MarginAccountOpenedDomainEvent(
            Guid.NewGuid(),
            marginAccountId,
            accountId.Trim(),
            currencyCode.Trim().ToUpperInvariant(),
            totalMargin,
            openedAtUtc);

        return new MarginAccount
        {
            Id = marginAccountId,
            AccountId = accountId.Trim(),
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(),
            TotalMargin = totalMargin,
            ReservedMargin = 0m,
            Version = 1,
            CreatedAt = openedAtUtc,
            UpdatedAt = openedAtUtc,
            DomainEvents = [openedEvent],
        };
    }

    public MarginAccount Reserve(MarginAccount marginAccount, decimal amount, DateTimeOffset reservedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(marginAccount);

        if (amount <= 0m)
        {
            throw new ArgumentException("Reserved margin amount must be greater than zero.", nameof(amount));
        }

        EnsureMutationTimestamp(marginAccount, reservedAtUtc, nameof(reservedAtUtc));

        var availableMargin = marginAccount.TotalMargin - marginAccount.ReservedMargin;
        if (amount > availableMargin)
        {
            throw new InvalidOperationException("Reserved margin cannot exceed available margin.");
        }

        marginAccount.ReservedMargin += amount;
        marginAccount.UpdatedAt = reservedAtUtc;
        marginAccount.Version = GetNextVersion(marginAccount.Version);
        AppendDomainEvent(marginAccount, new MarginReservedDomainEvent(Guid.NewGuid(), marginAccount.Id, amount, marginAccount.ReservedMargin, reservedAtUtc));

        return marginAccount;
    }

    public MarginAccount Release(MarginAccount marginAccount, decimal amount, DateTimeOffset releasedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(marginAccount);

        if (amount <= 0m)
        {
            throw new ArgumentException("Released margin amount must be greater than zero.", nameof(amount));
        }

        EnsureMutationTimestamp(marginAccount, releasedAtUtc, nameof(releasedAtUtc));

        if (amount > marginAccount.ReservedMargin)
        {
            throw new InvalidOperationException("Released margin cannot exceed reserved margin.");
        }

        marginAccount.ReservedMargin -= amount;
        marginAccount.UpdatedAt = releasedAtUtc;
        marginAccount.Version = GetNextVersion(marginAccount.Version);
        AppendDomainEvent(marginAccount, new MarginReleasedDomainEvent(Guid.NewGuid(), marginAccount.Id, amount, marginAccount.ReservedMargin, releasedAtUtc));

        return marginAccount;
    }

    private static void EnsureMutationTimestamp(MarginAccount marginAccount, DateTimeOffset occurredAtUtc, string parameterName)
    {
        if (occurredAtUtc < marginAccount.UpdatedAt)
        {
            throw new ArgumentException("Margin mutation timestamp cannot move backwards.", parameterName);
        }
    }

    private static void AppendDomainEvent(MarginAccount marginAccount, IDomainEvent domainEvent)
    {
        marginAccount.DomainEvents = marginAccount.DomainEvents
            .Concat([domainEvent])
            .ToArray();
    }

    private static int GetNextVersion(int currentVersion)
    {
        return currentVersion == int.MaxValue ? 1 : currentVersion + 1;
    }
}