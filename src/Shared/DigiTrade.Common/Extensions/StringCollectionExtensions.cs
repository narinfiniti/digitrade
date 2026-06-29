using System.Collections.Specialized;
using System.Diagnostics;

namespace DigiTrade.Common.Extensions;

/// <summary>
/// StringCollectionExtensions
/// </summary>
public static class StringCollectionExtensions
{
    [DebuggerStepThrough]
    public static NameValueCollection AsNameValueCollection(this IEnumerable<KeyValuePair<string, string>> collection)
    {
        var nv = new NameValueCollection();

        foreach (var field in collection)
        {
            nv.Add(field.Key, field.Value);
        }

        return nv;
    }

    [DebuggerStepThrough]
    public static NameValueCollection AsNameValueCollection(this IDictionary<string, string> collection)
    {
        var nv = new NameValueCollection();

        foreach (var field in collection)
        {
            nv.Add(field.Key, field.Value);
        }

        return nv;
    }
}