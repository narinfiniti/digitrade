using System.ComponentModel.DataAnnotations;

namespace DigiTrade.Common.Attributes;

/// <summary>
/// DecimalRangeAttribute.
/// </summary>
public class DecimalRangeAttribute : ValidationAttribute
{
    private readonly decimal _minValue;
    private readonly decimal _maxValue;

    public DecimalRangeAttribute()
    {
        _minValue = 0m;
        _maxValue = 999_999_999_999_999.9999m;
    }
    public DecimalRangeAttribute(double minValue, double maxValue)
    {
        _minValue = (decimal)minValue;
        _maxValue = (decimal)maxValue;
    }

    public override bool IsValid(object? value)
    {
        if(value == null)
        {
            return true;
        }

        if(value is decimal decimalValue)
        {
            return decimalValue >= _minValue && decimalValue <= _maxValue;
        }

        return false;
    }

    public override string FormatErrorMessage(string name)
    {
        return $"Invalid Target {name}; must be between {_minValue} and {_maxValue}.";
    }
}