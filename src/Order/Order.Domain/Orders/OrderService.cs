using DigiTrade.SharedKernel.Events;
using Order.Domain.Orders.Events;

namespace Order.Domain.Orders;

public sealed class OrderService : IOrderService
{
    public Order Place(
        string accountId,
        string instrumentId,
        OrderDirection direction,
        decimal quantity,
        decimal requestedPrice,
        DateTimeOffset submittedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            throw new ArgumentException("Order account id is required.", nameof(accountId));
        }

        if (string.IsNullOrWhiteSpace(instrumentId))
        {
            throw new ArgumentException("Order instrument id is required.", nameof(instrumentId));
        }

        if (quantity <= 0m)
        {
            throw new ArgumentException("Order quantity must be greater than zero.", nameof(quantity));
        }

        if (requestedPrice <= 0m)
        {
            throw new ArgumentException("Order requested price must be greater than zero.", nameof(requestedPrice));
        }

        var orderId = Guid.NewGuid();
        var placedEvent = new OrderPlacedDomainEvent(
            Guid.NewGuid(),
            orderId,
            accountId.Trim(),
            instrumentId.Trim(),
            direction,
            quantity,
            requestedPrice,
            submittedAtUtc);

        return new Order
        {
            Id = orderId,
            AccountId = accountId.Trim(),
            InstrumentId = instrumentId.Trim(),
            Direction = direction,
            Quantity = quantity,
            RequestedPrice = requestedPrice,
            Status = OrderStatus.PendingRiskApproval,
            SubmittedAtUtc = submittedAtUtc,
            Version = 1,
            CreatedAt = submittedAtUtc,
            UpdatedAt = submittedAtUtc,
            DomainEvents = [placedEvent],
        };
    }

    public Order Accept(Order order, DateTimeOffset acceptedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(order);
        EnsurePendingRiskApproval(order, "Only pending orders can be accepted.");
        EnsureMutationTimestamp(order, acceptedAtUtc, nameof(acceptedAtUtc));

        order.Status = OrderStatus.Accepted;
        order.AcceptedAtUtc = acceptedAtUtc;
        order.UpdatedAt = acceptedAtUtc;
        order.Version = GetNextVersion(order.Version);
        AppendDomainEvent(order, new OrderAcceptedDomainEvent(Guid.NewGuid(), order.Id, acceptedAtUtc));

        return order;
    }

    public Order Reject(Order order, DateTimeOffset rejectedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(order);
        EnsurePendingRiskApproval(order, "Only pending orders can be rejected.");
        EnsureMutationTimestamp(order, rejectedAtUtc, nameof(rejectedAtUtc));

        order.Status = OrderStatus.Rejected;
        order.RejectedAtUtc = rejectedAtUtc;
        order.UpdatedAt = rejectedAtUtc;
        order.Version = GetNextVersion(order.Version);
        AppendDomainEvent(order, new OrderRejectedDomainEvent(Guid.NewGuid(), order.Id, rejectedAtUtc));

        return order;
    }

    public Order Cancel(Order order, DateTimeOffset cancelledAtUtc)
    {
        ArgumentNullException.ThrowIfNull(order);

        if (order.Status == OrderStatus.Rejected)
        {
            throw new InvalidOperationException("Rejected orders cannot be cancelled.");
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            throw new InvalidOperationException("Order is already cancelled.");
        }

        EnsureMutationTimestamp(order, cancelledAtUtc, nameof(cancelledAtUtc));

        order.Status = OrderStatus.Cancelled;
        order.CancelledAtUtc = cancelledAtUtc;
        order.UpdatedAt = cancelledAtUtc;
        order.Version = GetNextVersion(order.Version);
        AppendDomainEvent(order, new OrderCancelledDomainEvent(Guid.NewGuid(), order.Id, cancelledAtUtc));

        return order;
    }

    private static void EnsurePendingRiskApproval(Order order, string errorMessage)
    {
        if (order.Status != OrderStatus.PendingRiskApproval)
        {
            throw new InvalidOperationException(errorMessage);
        }
    }

    private static void EnsureMutationTimestamp(Order order, DateTimeOffset occurredAtUtc, string parameterName)
    {
        if (occurredAtUtc < order.UpdatedAt)
        {
            throw new ArgumentException("Order mutation timestamp cannot move backwards.", parameterName);
        }
    }

    private static void AppendDomainEvent(Order order, IDomainEvent domainEvent)
    {
        order.DomainEvents = order.DomainEvents
            .Concat([domainEvent])
            .ToArray();
    }

    private static int GetNextVersion(int currentVersion)
    {
        return currentVersion == int.MaxValue ? 1 : currentVersion + 1;
    }
}