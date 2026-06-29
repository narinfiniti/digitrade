namespace DigiTrade.Security.Contracts;

public interface IPasswordHasher
{
    string Hash(string password);

    PasswordVerificationResult Verify(string password, string passwordHash);
}