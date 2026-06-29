namespace DigiTrade.Common.Models;

/// <summary>
/// Range model type.
/// </summary>
public class Range<T>
{
    public required T Start { get; set; }
    public required T End { get; set; }

    public Range() { }
    public Range(T start, T end)
    {
        Start = start;
        End = end;
    }

    public override string ToString()
    {
        return $"[{Start},{End}]";
    }
}