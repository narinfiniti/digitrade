using Identity.Application.Abstractions;

namespace Identity.Infrastructure.Options;

public sealed class IdentityTokenOptions : IIdentityAuthenticationSettings
{
    public string Issuer { get; set; } = "DigiTrade.IdentityService";

    public string Audience { get; set; } = "DigiTrade.Platform";

    public string SigningKey { get; set; } = "digitrade-local-development-signing-key-change-before-production";

    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromHours(1);

    public string[] DefaultScopes { get; set; } = ["platform"];

    IReadOnlyCollection<string> IIdentityAuthenticationSettings.DefaultScopes => DefaultScopes;
}