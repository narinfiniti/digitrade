using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace DigiTrade.SharedKernel.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddDigiTradeSwagger(
        this IServiceCollection services,
        IConfiguration configuration,
        string title,
        string description,
        string documentVersion = "v1")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        var tokenUrl = configuration["SWAGGER_OAUTH_TOKEN_URL"] ?? "/api/v1/identity/tokens/client-credentials";
        var scopeName = configuration["SWAGGER_OAUTH_SCOPE"] ?? "platform";

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(documentVersion, new OpenApiInfo
            {
                Title = title,
                Version = documentVersion,
                Description = description,
            });

            options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    ClientCredentials = new OpenApiOAuthFlow
                    {
                        TokenUrl = new Uri(tokenUrl, UriKind.RelativeOrAbsolute),
                        Scopes = new Dictionary<string, string>(StringComparer.Ordinal)
                        {
                            [scopeName] = "DigiTrade API scope",
                        },
                    },
                },
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "oauth2",
                        },
                    },
                    [scopeName]
                },
            });
        });

        return services;
    }

    public static WebApplication UseDigiTradeSwagger(
        this WebApplication app,
        string swaggerUiName,
        string documentVersion = "v1",
        string routePrefix = "swagger")
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentException.ThrowIfNullOrWhiteSpace(swaggerUiName);

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint($"{documentVersion}/swagger.json", swaggerUiName);
            options.RoutePrefix = routePrefix;
            options.OAuthClientId(app.Configuration["SWAGGER_OAUTH_CLIENT_ID"]);
            options.OAuthClientSecret(app.Configuration["SWAGGER_OAUTH_CLIENT_SECRET"]);
            options.OAuthScopes(app.Configuration["SWAGGER_OAUTH_SCOPE"] ?? "platform");
            options.OAuthUseBasicAuthenticationWithAccessCodeGrant();
        });

        return app;
    }
}
