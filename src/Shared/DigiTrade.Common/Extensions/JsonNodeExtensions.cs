using DigiTrade.Common.JsonOptions;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DigiTrade.Common.Extensions;

/// <summary>
/// JsonNodeExtensions.
/// </summary>
public static class JsonNodeExtensions
{
    public static TValue? As<TValue>(this JsonNode node)
    {
        var options = JsonSettingProvider.JsonSerializerOps;
        return node.Deserialize<TValue>(options);
    }

    public static bool IsEqual(this JsonNode a, JsonNode b)
    {
        var options = JsonSettingProvider.JsonSerializerOps;
        var o1 = a.AsObject().ToImmutableSortedDictionary(e => e.Key, e => e.Value);
        var o2 = b.AsObject().ToImmutableSortedDictionary(e => e.Key, e => e.Value);

        if(o1.Count != o2.Count)
        {
            return false;
        }

        return JsonSerializer.Serialize(o1, options).Is(JsonSerializer.Serialize(o2, options));
    }

    public static bool AreKeysEqual(this JsonNode a, JsonNode b)
    {
        var options = JsonSettingProvider.JsonSerializerOps;
        var o1 = a.AsObject().ToImmutableSortedDictionary(e => e.Key, e => e.Value);
        var o2 = b.AsObject().ToImmutableSortedDictionary(e => e.Key, e => e.Value);

        if(o1.Count != o2.Count)
        {
            return false;
        }

        return JsonSerializer.Serialize(o1.Keys, options).Is(JsonSerializer.Serialize(o2.Keys, options));
    }

    public static bool HasEmptyValue(this JsonNode a)
    {
        return a.AsObject().Any(e => e.Value is null || e.Value.ToString() == "");
    }

    public static bool HasDuplicateKeys(this JsonNode a)
    {
        var encounteredKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var documentNode = JsonDocument.Parse(a.ToString());
        foreach(var property in documentNode.RootElement.EnumerateObject())
        {
            if(!encounteredKeys.Add(property.Name))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsEmpty(this JsonNode a)
    {
        var emptyObject = JsonSerializer.Serialize(new JsonObject());
        return emptyObject.Equals(a.ToString(), StringComparison.Ordinal);
    }
}