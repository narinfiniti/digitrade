using System.Text.Json;
using DigiTrade.Common.JsonOptions;

namespace DigiTrade.Common.Models;

/// <summary>
/// Items as comma joined string and vice versa.
/// </summary>
public class Items<T> : List<T>, IItems where T : class
{
    private static readonly JsonOps JsonOps = new();
    public Items()
    { }

    public Items(IEnumerable<T> items) : base(items)
    { }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, JsonOps.Serialize);
    }

    public static implicit operator Items<T>(T[] value)
    {
        return new Items<T>(value);
    }
    public static implicit operator T[](Items<T> value)
    {
        return [.. value];
    }
}

public interface IItems { }

public static class ItemsExtensions
{
    private static readonly JsonOps JsonOps = new();

    public static Items<T> FromString<T>(this string value) where T : class
    {
        var items = JsonSerializer.Deserialize<IEnumerable<T>>(value, JsonOps.Serialize);
        return new Items<T>(items ?? Enumerable.Empty<T>());
    }
}