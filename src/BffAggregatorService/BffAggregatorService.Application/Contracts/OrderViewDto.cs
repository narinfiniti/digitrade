namespace BffAggregatorService.Application.Contracts;

public sealed record OrderViewDto(
    string OrderId,
    string AccountId,
    string InstrumentId,
    decimal Quantity,
    decimal Price,
    string Status,
    DateTimeOffset UpdatedAt);