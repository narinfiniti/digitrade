using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace DigiTrade.Common.Extensions;

/// <summary>
/// Enum Extensions.
/// </summary>
public static class EnumExtensions
{
    public static string? GetDisplayName(Enum enumValue)
    {
        return enumValue.GetType()
            .GetMember(enumValue.ToString())
            .FirstOrDefault()?
            .GetCustomAttribute<DisplayAttribute>()?
            .GetName();
    }

    public static string? GetDescription(Enum enumValue)
    {
        return enumValue.GetType()
            .GetMember(enumValue.ToString())
            .FirstOrDefault()?
            .GetCustomAttribute<DescriptionAttribute>()?
            .Description;
    }

    public static List<T> GetAllValues<T>() where T : Enum
    {
        return Enum.GetValues(typeof(T)).Cast<T>().ToList();
    }
}