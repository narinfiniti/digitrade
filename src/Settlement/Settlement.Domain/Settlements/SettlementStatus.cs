namespace Settlement.Domain.Settlements;

public enum SettlementStatus
{
    PendingFinalization = 1,
    Finalized = 2,
    Failed = 3,
}