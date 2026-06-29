namespace BffAggregatorService.Application.Contracts;

public sealed record AccountSummaryDto(
    string AccountId,
    decimal AvailableBalance,
    decimal ReservedBalance,
    decimal MarginBalance,
    IReadOnlyCollection<string> SourceServices,
    bool IsComplete);