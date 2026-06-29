namespace Risk.Application.Models;

public sealed record OrderSnapshotModel(
    Guid OrderId,
    string AccountId,
    string InstrumentId,
    ExternalOrderDirection Direction,
    ExternalOrderStatus Status,
    decimal Quantity,
    decimal RequestedPrice,
    DateTimeOffset SubmittedAtUtc,
    DateTimeOffset? AcceptedAtUtc,
    DateTimeOffset? RejectedAtUtc,
    DateTimeOffset? CancelledAtUtc,
    int Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);