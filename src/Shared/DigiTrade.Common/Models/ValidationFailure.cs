namespace DigiTrade.Common.Models;

/// <summary>
/// Validation failure.
/// </summary>
public class ValidationFailure(string propertyName, string errorMessage)
{
    public string PropertyName { get; set; } = propertyName;
    public string ErrorMessage { get; set; } = errorMessage;
}