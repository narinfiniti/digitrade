namespace DigiTrade.Common.Extensions;

/// <summary>
/// Extensions for generic type.
/// </summary>
public static class GenericTypeExtensions
{
    public static string GetGenericTypeName(this Type type)
    {
        string typeName;

        if(type.IsGenericType)
        {
            var genericTypes = string.Join(",", type.GetGenericArguments().Select(t => t.Name).ToArray());
            typeName = $"{type.Name.Remove(type.Name.IndexOf('`'))}<{genericTypes}>";
        }
        else
        {
            typeName = type.Name;
        }

        return typeName;
    }

    public static string GetGenericTypeName(this object o)
    {
        return o.GetType().GetGenericTypeName();
    }
}