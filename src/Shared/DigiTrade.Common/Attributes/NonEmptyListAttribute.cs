using System.Collections;
using System.ComponentModel.DataAnnotations;
using DigiTrade.Common.Extensions;

namespace DigiTrade.Common.Attributes;

/// <summary>
/// Non empty list Attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class NonEmptyListAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if(value is IEnumerable collection)
        {
            // if empty not valid
            if(collection.IsEmpty())
                return new ValidationResult($"Empty array: {validationContext.MemberName}.");

            return ValidationResult.Success;
        }

        return new ValidationResult("Invalid list!");
    }
}