namespace Identity.Api.Contracts;

public sealed record ClientCredentialsTokenInput(string ClientId, string ClientSecret, string? Scope = null);
