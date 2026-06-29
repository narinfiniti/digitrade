namespace DigiTrade.Security.Contracts;

public sealed record IssuedAccessToken(string AccessToken, DateTimeOffset ExpiresAtUtc, string TokenType = "Bearer");