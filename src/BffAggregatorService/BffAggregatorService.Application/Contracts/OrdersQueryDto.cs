namespace BffAggregatorService.Application.Contracts;

public sealed record OrdersQueryDto(
    IReadOnlyCollection<OrderViewDto> Orders,
    IReadOnlyCollection<string> SourceServices,
    bool IsComplete);