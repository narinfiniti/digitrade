namespace DigiTrade.Security.Contracts;

public interface ITokenIssuer
{
    Task<IssuedAccessToken> IssueAsync(AccessTokenDescriptor descriptor, CancellationToken cancellationToken = default);
}