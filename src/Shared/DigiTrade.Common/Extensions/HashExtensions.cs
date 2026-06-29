using System.Security.Cryptography;
using System.Text;

namespace DigiTrade.Common.Extensions;

/// <summary>
/// Extension methods for hashing strings
/// </summary>
public static class HashExtensions
{
    /// <summary>
    /// Creates a SHA256 hash of the specified input.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <returns>A hash</returns>
    public static string Sha256(this string input)
  {
    if (input.IsEmpty())
      return string.Empty;
    var bytes = Encoding.UTF8.GetBytes(input);
    var hash = SHA256.HashData(bytes);

    return Convert.ToBase64String(hash);
  }
  /// <summary>
  /// Creates a SHA512 hash of the specified input.
  /// </summary>
  /// <param name="input">The input.</param>
  /// <returns>A hash</returns>
  public static string Sha512(this string input)
  {
    if (input.IsEmpty())
      return string.Empty;
    var bytes = Encoding.UTF8.GetBytes(input);
    var hash = SHA512.HashData(bytes);

    return Convert.ToBase64String(hash);
  }

  public static string Salt(this int maxSaltLength)
    {
        var salt = new byte[maxSaltLength];
        using (var random = RandomNumberGenerator.Create())
        {
            random.GetNonZeroBytes(salt);
        }
        return Convert.ToBase64String(salt);
    }

    public static string Sha256Hash(this string input, string salt)
    {
        return Sha256(salt + input);
    }
}