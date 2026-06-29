using System.Collections;
using System.ComponentModel.DataAnnotations;
using DigiTrade.Common.Extensions;

namespace DigiTrade.Common.Attributes;

/// <summary>
/// Uri list Attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class UriListAttribute : ValidationAttribute
{
    public bool AllowEmpty { get; set; } = true;
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if(value is IEnumerable collection)
        {
            // if empty not valid
            if(!AllowEmpty && collection.IsEmpty())
                return new ValidationResult($"Empty url array: {validationContext.MemberName}.");

            var attr = new UrlAttribute();

            foreach (var item in collection)
            {
                // if all are not valid emails, then not valid
                if(!attr.IsValid(item))
                {
                    return new ValidationResult($"Invalid url: {item}.");
                }
            }

            return null;
        }

        return new ValidationResult("Invalid list!");
    }
}