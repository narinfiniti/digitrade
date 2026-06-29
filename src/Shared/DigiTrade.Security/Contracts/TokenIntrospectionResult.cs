namespace DigiTrade.Security.Contracts;

public sealed record TokenIntrospectionResult(
    bool IsActive,
    string? SubjectId,
    DateTimeOffset? ExpiresAtUtc,
    IReadOnlyCollection<string> Scopes,
    IReadOnlyDictionary<string, string> Claims);