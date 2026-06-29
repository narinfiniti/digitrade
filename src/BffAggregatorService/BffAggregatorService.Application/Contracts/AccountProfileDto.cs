namespace BffAggregatorService.Application.Contracts;

public sealed record AccountProfileDto(
    string AccountId,
    string UserId,
    string Tier,
    string BaseCurrency,
    IReadOnlyCollection<string> SourceServices,
    bool IsComplete);