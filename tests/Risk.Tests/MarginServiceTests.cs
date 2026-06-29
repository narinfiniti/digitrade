using Risk.Domain.Margins;
using Risk.Domain.Margins.Events;
using Xunit;

namespace Risk.Tests;

public sealed class MarginServiceTests
{
    private readonly MarginService marginService = new();

    [Fact]
    public void OpenCreatesMarginAccountAndOpenedEvent()
    {
        var openedAtUtc = new DateTimeOffset(2026, 5, 29, 10, 0, 0, TimeSpan.Zero);

        var marginAccount = marginService.Open("acct-1", "usd", 1000m, openedAtUtc);

        Assert.Equal("acct-1", marginAccount.AccountId);
        Assert.Equal("USD", marginAccount.CurrencyCode);
        Assert.Equal(1000m, marginAccount.TotalMargin);
        Assert.Equal(0m, marginAccount.ReservedMargin);
        Assert.Equal(1, marginAccount.Version);
        Assert.Equal(openedAtUtc, marginAccount.CreatedAt);
        Assert.Equal(openedAtUtc, marginAccount.UpdatedAt);

        var openedEvent = Assert.IsType<MarginAccountOpenedDomainEvent>(Assert.Single(marginAccount.DomainEvents));
        Assert.Equal(marginAccount.Id, openedEvent.MarginAccountId);
        Assert.Equal(marginAccount.CurrencyCode, openedEvent.CurrencyCode);
        Assert.Equal(marginAccount.TotalMargin, openedEvent.TotalMargin);
    }

    [Fact]
    public void ReserveAppendsReservedEventAndIncrementsVersion()
    {
        var openedAtUtc = new DateTimeOffset(2026, 5, 29, 10, 0, 0, TimeSpan.Zero);
        var reservedAtUtc = openedAtUtc.AddMinutes(1);
        var marginAccount = marginService.Open("acct-1", "USD", 1000m, openedAtUtc);

        marginService.Reserve(marginAccount, 250m, reservedAtUtc);

        Assert.Equal(250m, marginAccount.ReservedMargin);
        Assert.Equal(2, marginAccount.Version);
        Assert.Equal(reservedAtUtc, marginAccount.UpdatedAt);
        Assert.Contains(marginAccount.DomainEvents, domainEvent => domainEvent is MarginReservedDomainEvent reservedEvent && reservedEvent.Amount == 250m);
    }

    [Fact]
    public void ReleaseAppendsReleasedEventAndDecrementsReservedMargin()
    {
        var openedAtUtc = new DateTimeOffset(2026, 5, 29, 10, 0, 0, TimeSpan.Zero);
        var reservedAtUtc = openedAtUtc.AddMinutes(1);
        var releasedAtUtc = openedAtUtc.AddMinutes(2);
        var marginAccount = marginService.Open("acct-1", "USD", 1000m, openedAtUtc);

        marginService.Reserve(marginAccount, 300m, reservedAtUtc);
        marginService.Release(marginAccount, 125m, releasedAtUtc);

        Assert.Equal(175m, marginAccount.ReservedMargin);
        Assert.Equal(3, marginAccount.Version);
        Assert.Equal(releasedAtUtc, marginAccount.UpdatedAt);
        Assert.Contains(marginAccount.DomainEvents, domainEvent => domainEvent is MarginReleasedDomainEvent releasedEvent && releasedEvent.Amount == 125m);
    }

    [Fact]
    public void ReserveCannotExceedAvailableMargin()
    {
        var openedAtUtc = new DateTimeOffset(2026, 5, 29, 10, 0, 0, TimeSpan.Zero);
        var marginAccount = marginService.Open("acct-1", "USD", 100m, openedAtUtc);

        var exception = Assert.Throws<InvalidOperationException>(() => marginService.Reserve(marginAccount, 150m, openedAtUtc.AddMinutes(1)));

        Assert.Equal("Reserved margin cannot exceed available margin.", exception.Message);
    }

    [Fact]
    public void ReleaseCannotExceedReservedMargin()
    {
        var openedAtUtc = new DateTimeOffset(2026, 5, 29, 10, 0, 0, TimeSpan.Zero);
        var marginAccount = marginService.Open("acct-1", "USD", 100m, openedAtUtc);

        var exception = Assert.Throws<InvalidOperationException>(() => marginService.Release(marginAccount, 1m, openedAtUtc.AddMinutes(1)));

        Assert.Equal("Released margin cannot exceed reserved margin.", exception.Message);
    }
}