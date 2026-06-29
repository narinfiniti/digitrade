namespace DigiTrade.Security.Contracts;

public interface ITokenIntrospectionService
{
    Task<TokenIntrospectionResult> IntrospectAsync(string token, CancellationToken cancellationToken = default);
}