namespace DigiTrade.Security.Contracts;

public enum PasswordVerificationResult
{
    Failed = 0,
    Succeeded = 1,
    SuccessRehashNeeded = 2,
}