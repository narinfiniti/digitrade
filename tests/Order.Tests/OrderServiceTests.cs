using Order.Domain.Orders;
using Order.Domain.Orders.Events;
using Xunit;

namespace Order.Tests;

public sealed class OrderServiceTests
{
    private readonly OrderService orderService = new();

    [Fact]
    public void PlaceCreatesPendingRiskApprovalOrderAndPlacedEvent()
    {
        var submittedAtUtc = new DateTimeOffset(2026, 5, 29, 9, 0, 0, TimeSpan.Zero);

        var order = orderService.Place("acct-1", "EURUSD", OrderDirection.Buy, 1.5m, 1.2345m, submittedAtUtc);

        Assert.Equal(OrderStatus.PendingRiskApproval, order.Status);
        Assert.Equal(1, order.Version);
        Assert.Equal(submittedAtUtc, order.SubmittedAtUtc);
        Assert.Equal(submittedAtUtc, order.CreatedAt);
        Assert.Equal(submittedAtUtc, order.UpdatedAt);

        var placedEvent = Assert.IsType<OrderPlacedDomainEvent>(Assert.Single(order.DomainEvents));
        Assert.Equal(order.Id, placedEvent.OrderId);
        Assert.Equal(order.AccountId, placedEvent.AccountId);
        Assert.Equal(order.InstrumentId, placedEvent.InstrumentId);
        Assert.Equal(order.RequestedPrice, placedEvent.RequestedPrice);
    }

    [Fact]
    public void AcceptTransitionsPendingOrderAndAppendsAcceptedEvent()
    {
        var submittedAtUtc = new DateTimeOffset(2026, 5, 29, 9, 0, 0, TimeSpan.Zero);
        var acceptedAtUtc = submittedAtUtc.AddMinutes(2);
        var order = orderService.Place("acct-1", "EURUSD", OrderDirection.Sell, 2m, 1.2100m, submittedAtUtc);

        orderService.Accept(order, acceptedAtUtc);

        Assert.Equal(OrderStatus.Accepted, order.Status);
        Assert.Equal(2, order.Version);
        Assert.Equal(acceptedAtUtc, order.AcceptedAtUtc);
        Assert.Equal(acceptedAtUtc, order.UpdatedAt);
        Assert.Contains(order.DomainEvents, domainEvent => domainEvent is OrderAcceptedDomainEvent acceptedEvent && acceptedEvent.OrderId == order.Id);
    }

    [Fact]
    public void CancelAllowsAcceptedOrderAndAppendsCancelledEvent()
    {
        var submittedAtUtc = new DateTimeOffset(2026, 5, 29, 9, 0, 0, TimeSpan.Zero);
        var acceptedAtUtc = submittedAtUtc.AddMinutes(1);
        var cancelledAtUtc = submittedAtUtc.AddMinutes(2);
        var order = orderService.Place("acct-1", "EURUSD", OrderDirection.Buy, 1m, 1.2400m, submittedAtUtc);

        orderService.Accept(order, acceptedAtUtc);
        orderService.Cancel(order, cancelledAtUtc);

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(3, order.Version);
        Assert.Equal(cancelledAtUtc, order.CancelledAtUtc);
        Assert.Contains(order.DomainEvents, domainEvent => domainEvent is OrderCancelledDomainEvent cancelledEvent && cancelledEvent.OrderId == order.Id);
    }

    [Fact]
    public void RejectMakesOrderTerminalForLaterCancellation()
    {
        var submittedAtUtc = new DateTimeOffset(2026, 5, 29, 9, 0, 0, TimeSpan.Zero);
        var rejectedAtUtc = submittedAtUtc.AddMinutes(1);
        var order = orderService.Place("acct-1", "EURUSD", OrderDirection.Buy, 1m, 1.2400m, submittedAtUtc);

        orderService.Reject(order, rejectedAtUtc);

        var exception = Assert.Throws<InvalidOperationException>(() => orderService.Cancel(order, rejectedAtUtc.AddMinutes(1)));
        Assert.Equal("Rejected orders cannot be cancelled.", exception.Message);
    }
}