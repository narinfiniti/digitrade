using AutoMapper;
using DigiTrade.SharedKernel.Filters;
using DigiTrade.SharedKernel.Models.Response;
using DigiTrade.Security.Contracts;
using FluentValidation;
using Identity.Api.Contracts;
using Identity.Application.Abstractions;
using Identity.Application.Errors;
using Identity.Application.Models;
using Identity.Application.UseCases;
using MediatR;

namespace Identity.Api.Endpoints;

public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/api/v1/identity");
        group.MapPost("/users", RegisterUserAsync).AddEndpointFilter<ResponseResultFilter>();
        group.MapPost("/tokens", IssueAccessTokenAsync).AddEndpointFilter<ResponseResultFilter>();
        group.MapPost("/tokens/client-credentials", IssueClientCredentialsTokenAsync).AddEndpointFilter<ResponseResultFilter>();
        group.MapPost("/tokens/introspect", IntrospectAccessTokenAsync).AddEndpointFilter<ResponseResultFilter>();

        return endpoints;
    }

    private static async Task<IResult> RegisterUserAsync(
        RegisterUserInput request,
        IMapper mapper,
        IMediator mediator,
        IValidator<RegisterUserCommand.Model> validator,
        CancellationToken cancellationToken)
    {
        var model = new RegisterUserCommand.Model(request.UserName, request.Email, request.Password);
        var validationResult = await validator.ValidateAsync(model, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        RegisteredUserResultModel result;

        try
        {
            result = await mediator.Send(new RegisterUserCommand(model), cancellationToken);
        }
        catch (ArgumentException exception) when (TryMapProblem(exception.Message, out var problem))
        {
            return problem;
        }

        if (result?.UserId is null)
        {
            return Results.BadRequest($"Registration failed for: {request?.UserName}");
        }

        return Results.Created($"/api/v1/identity/users/{result.UserId}", mapper.Map<RegisterUserDto>(result));
    }

    private static async Task<IResult> IssueAccessTokenAsync(
        IssueAccessTokenInput request,
        IMapper mapper,
        IMediator mediator,
        IValidator<IssueAccessTokenCommand.Model> validator,
        CancellationToken cancellationToken)
    {
        var model = new IssueAccessTokenCommand.Model(request.Login, request.Password);
        var validationResult = await validator.ValidateAsync(model, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        IssuedAccessToken result;

        try
        {
            result = await mediator.Send(new IssueAccessTokenCommand(model), cancellationToken);
        }
        catch (ArgumentException exception) when (TryMapProblem(exception.Message, out var problem))
        {
            return problem;
        }

        if (result?.AccessToken is null)
        {
            return Results.BadRequest($"Token issuance failed for: {request?.Login}");
        }

        return Results.Ok(mapper.Map<AccessTokenDto>(result));
    }

    private static async Task<IResult> IntrospectAccessTokenAsync(
        IntrospectAccessTokenInput request,
        ITokenIntrospectionService introspectionService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.AccessToken))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                [nameof(IntrospectAccessTokenInput.AccessToken)] = ["'Access Token' must not be empty."],
            });
        }

        var result = await introspectionService.IntrospectAsync(request.AccessToken, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> IssueClientCredentialsTokenAsync(
        ClientCredentialsTokenInput request,
        ITokenIssuer tokenIssuer,
        IIdentityAuthenticationSettings authenticationSettings,
        IMapper mapper,
        IConfiguration configuration,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ClientId) || string.IsNullOrWhiteSpace(request.ClientSecret))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                [nameof(ClientCredentialsTokenInput.ClientId)] = ["'Client Id' must not be empty."],
                [nameof(ClientCredentialsTokenInput.ClientSecret)] = ["'Client Secret' must not be empty."],
            });
        }

        var expectedClientId = configuration["SWAGGER_OAUTH_CLIENT_ID"];
        var expectedClientSecret = configuration["SWAGGER_OAUTH_CLIENT_SECRET"];
        if (string.IsNullOrWhiteSpace(expectedClientId) || string.IsNullOrWhiteSpace(expectedClientSecret))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "OAuth client credentials are not configured",
                detail: "SWAGGER_OAUTH_CLIENT_ID and SWAGGER_OAUTH_CLIENT_SECRET must be configured.");
        }

        if (!string.Equals(expectedClientId, request.ClientId, StringComparison.Ordinal)
            || !string.Equals(expectedClientSecret, request.ClientSecret, StringComparison.Ordinal))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Invalid client credentials",
                detail: "The supplied client credentials are invalid.");
        }

        var now = timeProvider.GetUtcNow();
        var requestedScopes = ParseScopes(request.Scope, authenticationSettings.DefaultScopes);
        var claims = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = request.ClientId,
        };

        var accessToken = await tokenIssuer.IssueAsync(
            new AccessTokenDescriptor(
                SubjectId: $"client:{request.ClientId}",
                UserName: request.ClientId,
                Scopes: requestedScopes,
                ExpiresAtUtc: now + authenticationSettings.AccessTokenLifetime,
                Claims: claims),
            cancellationToken);

        return Results.Ok(mapper.Map<AccessTokenDto>(accessToken));
    }

    private static IReadOnlyCollection<string> ParseScopes(string? scopeValue, IReadOnlyCollection<string> fallbackScopes)
    {
        if (string.IsNullOrWhiteSpace(scopeValue))
        {
            return fallbackScopes;
        }

        return scopeValue
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static bool TryMapProblem(string errorMessage, out IResult problem)
    {
        ArgumentException.ThrowIfNullOrEmpty(errorMessage);

        if (string.Equals(errorMessage, IdentityErrors.InvalidRegistrationInput.Error, StringComparison.Ordinal))
        {
            problem = ToProblem(IdentityErrors.InvalidRegistrationInput, "Invalid registration request");
            return true;
        }

        if (string.Equals(errorMessage, IdentityErrors.UserNameAlreadyTaken.Error, StringComparison.Ordinal))
        {
            problem = ToProblem(IdentityErrors.UserNameAlreadyTaken, "Registration conflict");
            return true;
        }

        if (string.Equals(errorMessage, IdentityErrors.EmailAlreadyTaken.Error, StringComparison.Ordinal))
        {
            problem = ToProblem(IdentityErrors.EmailAlreadyTaken, "Registration conflict");
            return true;
        }

        if (string.Equals(errorMessage, IdentityErrors.InvalidCredentials.Error, StringComparison.Ordinal))
        {
            problem = ToProblem(IdentityErrors.InvalidCredentials, "Authentication failed");
            return true;
        }

        problem = Results.Problem(statusCode: StatusCodes.Status500InternalServerError);
        return false;
    }

    private static IResult ToProblem(ErrorResult error, string title)
    {
        return Results.Problem(
            detail: error.Error,
            statusCode: error.Status,
            title: title,
            extensions: new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["code"] = GetErrorCode(error),
            });
    }

    private static string GetErrorCode(ErrorResult error)
    {
        ArgumentNullException.ThrowIfNull(error);

        var separatorIndex = error.Error.IndexOf(':');
        return separatorIndex > 0 ? error.Error[..separatorIndex] : error.Error;
    }
}