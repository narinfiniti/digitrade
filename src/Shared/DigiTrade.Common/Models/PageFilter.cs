using System.ComponentModel.DataAnnotations;

namespace DigiTrade.Common.Models;

/// <summary>
/// PageFilter.
/// </summary>
public class PageFilter
{
    [Range(0, int.MaxValue)] public int Skip { get; set; } = 0;
    [Range(1, int.MaxValue)] public int Page { get; set; } = 1;
    [Range(1, int.MaxValue)] public int Limit { get; set; } = 10;
}