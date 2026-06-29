namespace DigiTrade.Messaging.Persistence.Outbox;

public enum OutboxMessageStatus
{
    Pending = 0,
    Published = 1,
    Failed = 2,
    Processing = 3,
}