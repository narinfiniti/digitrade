using System.ComponentModel.DataAnnotations;

namespace DigiTrade.Common.Attributes;

/// <summary>
/// Enum of items Attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class EnumOfAttribute(Type type) : ValidationAttribute
{
    private Type Type { get; } = type;

    public override bool IsValid(object? value)
    {
        if(value == null)
            return true;

        return !uint.TryParse(value.ToString(), out _) &&
               Enum.TryParse(Type, value.ToString(), out _);
    }

    public override string FormatErrorMessage(string name)
    {
        return $@"{name} accepts values: [{string.Join(", ", Enum.GetNames(Type))}]";
    }
}