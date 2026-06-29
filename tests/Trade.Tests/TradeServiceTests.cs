using Trade.Domain.Trades;
using Trade.Domain.Trades.Events;
using Xunit;

namespace Trade.Tests;

public sealed class TradeServiceTests
{
    private readonly TradeService subject = new();

    [Fact]
    public void OpenAssignsInitialStateAndEmitsOpenedEvent()
    {
        var openedAtUtc = new DateTimeOffset(2026, 05, 28, 12, 00, 00, TimeSpan.Zero);

        var trade = subject.Open("acct-1", "EURUSD", TradeDirection.Buy, 2.5m, 1.23456m, openedAtUtc);

        Assert.NotEqual(Guid.Empty, trade.Id);
        Assert.Equal("acct-1", trade.AccountId);
        Assert.Equal("EURUSD", trade.InstrumentId);
        Assert.Equal(TradeStatus.Open, trade.Status);
        Assert.Equal(1, trade.Version);

        var domainEvent = Assert.Single(trade.DomainEvents.OfType<TradeOpenedDomainEvent>());
        Assert.Equal(trade.Id, domainEvent.TradeId);
        Assert.Equal(openedAtUtc, domainEvent.OccurredAtUtc);
    }

    [Fact]
    public void OpenWithNonPositiveQuantityThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => subject.Open(
            "acct-1",
            "EURUSD",
            TradeDirection.Buy,
            0m,
            1.23456m,
            new DateTimeOffset(2026, 05, 28, 12, 00, 00, TimeSpan.Zero)));

        Assert.Equal("quantity", exception.ParamName);
    }

    [Fact]
    public void CloseUpdatesStateAndEmitsClosedEvent()
    {
        var openedAtUtc = new DateTimeOffset(2026, 05, 28, 12, 00, 00, TimeSpan.Zero);
        var closedAtUtc = openedAtUtc.AddMinutes(5);
        var trade = subject.Open("acct-1", "EURUSD", TradeDirection.Sell, 1.2m, 1.11111m, openedAtUtc);

        subject.Close(trade, 1.10101m, closedAtUtc);

        Assert.Equal(TradeStatus.Closed, trade.Status);
        Assert.Equal(1.10101m, trade.ClosePrice);
        Assert.Equal(closedAtUtc, trade.ClosedAtUtc);
        Assert.Equal(2, trade.Version);

        var domainEvent = Assert.Single(trade.DomainEvents.OfType<TradeClosedDomainEvent>());
        Assert.Equal(trade.Id, domainEvent.TradeId);
        Assert.Equal(1.10101m, domainEvent.ClosePrice);
    }
}