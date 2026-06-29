namespace Order.Domain.Orders;

public enum OrderStatus
{
    PendingRiskApproval = 1,
    Accepted = 2,
    Rejected = 3,
    Cancelled = 4,
}