using System.Globalization;

namespace DigiTrade.Common.Extensions;

public static class DecimalExtensions
{
    public static int ForMagnatiPayment(this decimal amount)
    {
        string amountString = amount.ToString("F2", CultureInfo.InvariantCulture);
        string[] parts = amountString.Split('.');
    
        string left = parts[0];
        string right = parts.Length > 1 ? parts[1] : "00";

        string finalAmount = left;

        if (double.TryParse(right, out double rightValue) && rightValue > 0)
        {
            finalAmount = (left.Trim() + right.Trim());

            if (right.Trim().Length == 1)
            {
                finalAmount += "0";
            }
        }
        else
        {
            finalAmount += "00";
        }

        return int.TryParse(finalAmount, out int result) ? result : 0;
    }

    /// <summary>
    /// Removes integral digits and takes fractional digits.
    /// </summary>
    public static decimal FractionalDigits(this decimal value)
    {
        return value - decimal.Truncate(value);
    }
    public static bool HasFractionalDigits(this decimal value, byte maxDigits)
    {
        var mult = (decimal)Math.Pow(10, maxDigits);
        return (value.FractionalDigits() * mult).FractionalDigits() == 0;
    }
    public static decimal TakeFractionalDigits(this decimal value, byte digits)
    {
        var mult = (decimal)Math.Pow(10, digits);
        return decimal.Truncate(value) + decimal.Truncate(value.FractionalDigits() * mult) / mult;
    }
    public static decimal Truncate(this decimal value, int precision = 0)
    {
        // var step = (decimal) Math.Pow(10, precision);
        // var tmp = Math.Truncate(step * value);
        // return tmp / step;
        var pow = (decimal)Math.Pow(10, precision);
        return decimal.Truncate(value) + decimal.Truncate(value.FractionalDigits() * pow) / pow;
    }
    public static decimal GetValueOrDefault(this decimal? value)
    {
        return value ?? 0;
    }
}