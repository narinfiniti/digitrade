using DigiTrade.Security.Contracts;
using DigiTrade.SharedKernel.Abstractions;
using Identity.Application.Abstractions;
using Identity.Application.Errors;
using Identity.Application.Support;
using MediatR;

namespace Identity.Application.UseCases;

public sealed class IssueAccessTokenCommand(IssueAccessTokenCommand.Model? input)
    : IUseCase<IssueAccessTokenCommand.Model, IssuedAccessToken>
{
  public Model? Input => input;

  public sealed record Model(string Login, string Password);

    public sealed class Handler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenIssuer tokenIssuer,
        IIdentityAuthenticationSettings authenticationSettings,
        TimeProvider timeProvider) : IRequestHandler<IssueAccessTokenCommand, IssuedAccessToken>
    {
        public async Task<IssuedAccessToken> Handle(IssueAccessTokenCommand request, CancellationToken cancellationToken)
        {
            var input = request.Input;
            if (input is null || string.IsNullOrWhiteSpace(input.Login) || string.IsNullOrWhiteSpace(input.Password))
            {
                throw new ArgumentException(IdentityErrors.InvalidCredentials.Error);
            }

            var normalizedLogin = IdentityTextNormalizer.Normalize(input.Login);
            var user = await userRepository.FindByLoginAsync(normalizedLogin, cancellationToken);
            if (user is null)
            {
                throw new ArgumentException(IdentityErrors.InvalidCredentials.Error);
            }

            var verificationResult = passwordHasher.Verify(input.Password, user.PasswordHash);
            if (verificationResult == PasswordVerificationResult.Failed)
            {
                throw new ArgumentException(IdentityErrors.InvalidCredentials.Error);
            }

            var now = timeProvider.GetUtcNow();
            var expiresAtUtc = now + authenticationSettings.AccessTokenLifetime;
            var claims = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["email"] = user.Email,
            };

            var accessToken = await tokenIssuer.IssueAsync(
                new AccessTokenDescriptor(
                    user.Id.ToString(),
                    user.UserName,
                    authenticationSettings.DefaultScopes,
                    expiresAtUtc,
                    claims),
                cancellationToken);

            return accessToken;
        }
    }
}