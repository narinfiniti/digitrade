using System.Security.Cryptography;
using DigiTrade.Security.Contracts;

namespace Identity.Infrastructure.Security;

public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const string Version = "v1";
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100_000;

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);

        return string.Join('.', Version, Iterations, Convert.ToBase64String(salt), Convert.ToBase64String(hash));
    }

    public PasswordVerificationResult Verify(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash))
        {
            return PasswordVerificationResult.Failed;
        }

        var segments = passwordHash.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length != 4 || !string.Equals(segments[0], Version, StringComparison.Ordinal))
        {
            return PasswordVerificationResult.Failed;
        }

        if (!int.TryParse(segments[1], out var iterations) || iterations <= 0)
        {
            return PasswordVerificationResult.Failed;
        }

        byte[] salt;
        byte[] expectedHash;

        try
        {
            salt = Convert.FromBase64String(segments[2]);
            expectedHash = Convert.FromBase64String(segments[3]);
        }
        catch (FormatException)
        {
            return PasswordVerificationResult.Failed;
        }

        var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expectedHash.Length);
        if (!CryptographicOperations.FixedTimeEquals(actualHash, expectedHash))
        {
            return PasswordVerificationResult.Failed;
        }

        return iterations < Iterations
            ? PasswordVerificationResult.SuccessRehashNeeded
            : PasswordVerificationResult.Succeeded;
    }
}