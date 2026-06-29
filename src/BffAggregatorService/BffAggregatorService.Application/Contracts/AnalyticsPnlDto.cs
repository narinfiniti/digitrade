namespace BffAggregatorService.Application.Contracts;

public sealed record AnalyticsPnlDto(
    string AccountId,
    decimal TodayPnl,
    decimal WeekPnl,
    decimal MonthPnl,
    IReadOnlyCollection<string> SourceServices,
    bool IsComplete);