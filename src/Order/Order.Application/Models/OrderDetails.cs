using Order.Domain.Orders;

namespace Order.Application.Models;

public sealed record OrderDetailsModel(
    Guid OrderId,
    string AccountId,
    string InstrumentId,
    OrderDirection Direction,
    OrderStatus Status,
    decimal Quantity,
    decimal RequestedPrice,
    DateTimeOffset SubmittedAtUtc,
    DateTimeOffset? AcceptedAtUtc,
    DateTimeOffset? RejectedAtUtc,
    DateTimeOffset? CancelledAtUtc,
    int Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);