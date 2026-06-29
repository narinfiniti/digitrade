using System.Collections;
using System.ComponentModel.DataAnnotations;
using DigiTrade.Common.Extensions;

namespace DigiTrade.Common.Attributes;

/// <summary>
/// Email list Attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class EmailListAttribute : ValidationAttribute
{
    public bool AllowEmpty { get; set; } = true;
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if(value is IEnumerable collection)
        {
            // if empty not valid
            if(!AllowEmpty && collection.IsEmpty())
                return new ValidationResult("Empty email array: {0}.");

            var attr = new EmailAddressAttribute();

            foreach (var item in collection)
            {
                // if all are not valid emails, then not valid
                if(!attr.IsValid(item))
                {
                    return new ValidationResult($"Invalid email: {item}.");
                }
            }

            return ValidationResult.Success;
        }

        return new ValidationResult("Invalid list!");
    }
}