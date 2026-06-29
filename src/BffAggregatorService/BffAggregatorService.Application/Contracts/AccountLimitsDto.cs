namespace BffAggregatorService.Application.Contracts;

public sealed record AccountLimitsDto(
    string AccountId,
    decimal DailyNotionalLimit,
    decimal MaxOrderQuantity,
    decimal MaxLeverage,
    IReadOnlyCollection<string> SourceServices,
    bool IsComplete);