using System.ComponentModel.DataAnnotations;
using DigiTrade.Common.Extensions;

namespace DigiTrade.Common.Attributes;

/// <summary>
/// DecimalDigitAttribute.
/// </summary>
public class DecimalDigitAttribute(byte digits) : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if(value == null)
        {
            return true;
        }

        if(value.GetType() != typeof(decimal))
        {
            throw new ArgumentException("Invalid type: the argument is not of type decimal.");
        }

        var val = (decimal) value;
        return val.HasFractionalDigits(digits);
    }
}