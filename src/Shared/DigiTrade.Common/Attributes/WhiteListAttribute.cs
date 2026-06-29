using System.ComponentModel.DataAnnotations;

namespace DigiTrade.Common.Attributes;

/// <summary>
/// White list Attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class WhiteListAttribute(params object[] whiteList) : ValidationAttribute
{
    private HashSet<object> WhiteList { get; } = new(whiteList);

    public override bool IsValid(object? value)
    {
        return value != null && WhiteList.Contains(value);
    }

    public override string FormatErrorMessage(string name)
    {
        return $@"{name} accepts values: {string.Join(",", WhiteList)}";
    }
}