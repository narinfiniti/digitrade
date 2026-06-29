namespace Risk.Application.Models;

public enum ExternalOrderStatus
{
    PendingRiskApproval = 1,
    Accepted = 2,
    Rejected = 3,
    Cancelled = 4,
}