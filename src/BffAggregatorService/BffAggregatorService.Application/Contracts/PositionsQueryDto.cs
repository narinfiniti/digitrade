namespace BffAggregatorService.Application.Contracts;

public sealed record PositionsQueryDto(
    string AccountId,
    IReadOnlyCollection<PositionItemDto> Positions,
    IReadOnlyCollection<string> SourceServices,
    bool IsComplete);