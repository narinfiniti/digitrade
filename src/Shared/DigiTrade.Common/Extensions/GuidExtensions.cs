namespace DigiTrade.Common.Extensions;

public static class GuidExtensions
{
    public static string GenerateRandomStringFromGuid(this Guid value, int length = 8)
    {
        int seed = BitConverter.ToInt32(value.ToByteArray(), 0);
        var random = new Random(seed);
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var result = new char[length];
        for (int i = 0; i < length; i++)
        {
            result[i] = chars[random.Next(chars.Length)];
        }
        return new string(result);
    }

  public static bool IsEmpty(this Guid? value)
  {
    return value is null || value == Guid.Empty;
  }

  public static bool IsNotEmpty(this Guid? value)
  {
    return !value.IsEmpty();
  }

  public static bool IsEmpty(this Guid value)
  {
    return value == Guid.Empty;
  }

  public static bool IsNotEmpty(this Guid value)
  {
    return !value.IsEmpty();
  }
}