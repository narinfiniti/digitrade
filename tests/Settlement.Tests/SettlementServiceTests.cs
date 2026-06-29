using Settlement.Domain.Settlements;
using Settlement.Domain.Settlements.Events;
using Xunit;

namespace Settlement.Tests;

public sealed class SettlementServiceTests
{
    private readonly SettlementService subject = new();

    [Fact]
    public void InitiateAssignsPendingStateAndEmitsInitiatedEvent()
    {
        var tradeId = Guid.NewGuid();
        var initiatedAtUtc = new DateTimeOffset(2026, 05, 29, 12, 00, 00, TimeSpan.Zero);

        var settlement = subject.Initiate(tradeId, " acct-1 ", " usd ", 250.75m, initiatedAtUtc);

        Assert.NotEqual(Guid.Empty, settlement.Id);
        Assert.Equal(tradeId, settlement.TradeId);
        Assert.Equal("acct-1", settlement.AccountId);
        Assert.Equal("USD", settlement.CurrencyCode);
        Assert.Equal(250.75m, settlement.NetAmount);
        Assert.Equal(SettlementStatus.PendingFinalization, settlement.Status);
        Assert.Equal(1, settlement.Version);

        var domainEvent = Assert.Single(settlement.DomainEvents.OfType<SettlementInitiatedDomainEvent>());
        Assert.Equal(settlement.Id, domainEvent.SettlementId);
        Assert.Equal(tradeId, domainEvent.TradeId);
        Assert.Equal(250.75m, domainEvent.NetAmount);
        Assert.Equal(initiatedAtUtc, domainEvent.OccurredAtUtc);
    }

    [Fact]
    public void InitiateWithZeroNetAmountThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => subject.Initiate(
            Guid.NewGuid(),
            "acct-1",
            "USD",
            0m,
            new DateTimeOffset(2026, 05, 29, 12, 00, 00, TimeSpan.Zero)));

        Assert.Equal("netAmount", exception.ParamName);
    }

    [Fact]
    public void FinalizeUpdatesStateAndEmitsFinalizedEvent()
    {
        var initiatedAtUtc = new DateTimeOffset(2026, 05, 29, 12, 00, 00, TimeSpan.Zero);
        var finalizedAtUtc = initiatedAtUtc.AddMinutes(10);
        var settlement = subject.Initiate(Guid.NewGuid(), "acct-1", "USD", -35.10m, initiatedAtUtc);

        subject.Finalize(settlement, finalizedAtUtc);

        Assert.Equal(SettlementStatus.Finalized, settlement.Status);
        Assert.Equal(finalizedAtUtc, settlement.FinalizedAtUtc);
        Assert.Equal(2, settlement.Version);

        var domainEvent = Assert.Single(settlement.DomainEvents.OfType<SettlementFinalizedDomainEvent>());
        Assert.Equal(settlement.Id, domainEvent.SettlementId);
        Assert.Equal(finalizedAtUtc, domainEvent.OccurredAtUtc);
    }

    [Fact]
    public void FailUpdatesStateAndEmitsFailedEvent()
    {
        var initiatedAtUtc = new DateTimeOffset(2026, 05, 29, 12, 00, 00, TimeSpan.Zero);
        var failedAtUtc = initiatedAtUtc.AddMinutes(3);
        var settlement = subject.Initiate(Guid.NewGuid(), "acct-1", "USD", 89.40m, initiatedAtUtc);

        subject.Fail(settlement, " downstream settlement provider rejected the transfer ", failedAtUtc);

        Assert.Equal(SettlementStatus.Failed, settlement.Status);
        Assert.Equal(failedAtUtc, settlement.FailedAtUtc);
        Assert.Equal("downstream settlement provider rejected the transfer", settlement.FailureReason);
        Assert.Equal(2, settlement.Version);

        var domainEvent = Assert.Single(settlement.DomainEvents.OfType<SettlementFailedDomainEvent>());
        Assert.Equal(settlement.Id, domainEvent.SettlementId);
        Assert.Equal("downstream settlement provider rejected the transfer", domainEvent.FailureReason);
        Assert.Equal(failedAtUtc, domainEvent.OccurredAtUtc);
    }

    [Fact]
    public void FinalizeAfterFailureThrowsInvalidOperationException()
    {
        var initiatedAtUtc = new DateTimeOffset(2026, 05, 29, 12, 00, 00, TimeSpan.Zero);
        var settlement = subject.Initiate(Guid.NewGuid(), "acct-1", "USD", 125m, initiatedAtUtc);
        subject.Fail(settlement, "provider unavailable", initiatedAtUtc.AddMinutes(1));

        Assert.Throws<InvalidOperationException>(() => subject.Finalize(settlement, initiatedAtUtc.AddMinutes(2)));
    }
}