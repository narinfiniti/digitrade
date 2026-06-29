namespace Identity.Application.Abstractions;

public interface IIdentityAuthenticationSettings
{
    TimeSpan AccessTokenLifetime { get; }

    IReadOnlyCollection<string> DefaultScopes { get; }
}