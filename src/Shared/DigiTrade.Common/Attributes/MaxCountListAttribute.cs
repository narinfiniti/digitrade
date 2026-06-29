using System.Collections;
using System.ComponentModel.DataAnnotations;
using DigiTrade.Common.Extensions;

namespace DigiTrade.Common.Attributes;

/// <summary>
/// Max Count list Attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class MaxCountListAttribute(int maxCount) : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if(value is null) return ValidationResult.Success;
        if(value is IEnumerable collection)
        {
            if(collection.GetCount() > maxCount)
            {
                return new ValidationResult($"Maximum count is {maxCount}.");
            }
            return ValidationResult.Success;
        }

        return new ValidationResult("Invalid list!");
    }
}