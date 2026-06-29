namespace DigiTrade.Security.Contracts;

public sealed record AccessTokenDescriptor(
    string SubjectId,
    string UserName,
    IReadOnlyCollection<string> Scopes,
    DateTimeOffset ExpiresAtUtc,
    IReadOnlyDictionary<string, string> Claims);