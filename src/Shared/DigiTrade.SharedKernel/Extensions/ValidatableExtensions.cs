using FluentValidation.Results;

namespace DigiTrade.SharedKernel.Extensions;

/// <summary>
/// Validatable Extensions.
/// </summary>
public static class ValidatableExtensions
{
    public static Dictionary<string, string[]> ToDictionary(this ValidationResult validationResult)
    {
        return validationResult.Errors
            .GroupBy(error => error.PropertyName, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.ErrorMessage).Distinct(StringComparer.Ordinal).ToArray(),
                StringComparer.Ordinal);
    }
}