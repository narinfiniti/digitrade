using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Identity.Api.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Identity.Api.IntegrationTests;

public sealed class IdentityEndpointsTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions WebJsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task HealthEndpointsReturnOk()
    {
        using var client = factory.CreateClient();

        using var liveResponse = await client.GetAsync("/health/live");
        using var readyResponse = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, liveResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, readyResponse.StatusCode);
        Assert.Equal("Healthy", await liveResponse.Content.ReadAsStringAsync());
        Assert.Equal("Healthy", await readyResponse.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task RegisterUserWithInvalidRequestReturnsValidationProblem()
    {
        using var client = factory.CreateClient();
        using var response = await client.PostAsJsonAsync(
            "/api/v1/identity/users",
            new RegisterUserInput(string.Empty, string.Empty, string.Empty));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        using var document = await ReadJsonDocumentAsync(response);
        Assert.True(document.RootElement.TryGetProperty("error", out var errorProperty));
        Assert.False(string.IsNullOrWhiteSpace(errorProperty.GetString()));
    }

    [Fact]
    public async Task RegisterIssueAndIntrospectTokenWorksEndToEnd()
    {
        using var client = factory.CreateClient();
        var uniqueSuffix = Guid.NewGuid().ToString("N");
        var userName = $"phase2-user-{uniqueSuffix}";
        var email = $"phase2-user-{uniqueSuffix}@example.test";
        const string password = "Phase2Pass!123";
        var registrationRequest = new RegisterUserInput(userName, email, password);

        using var registrationResponse = await client.PostAsJsonAsync("/api/v1/identity/users", registrationRequest);
        Assert.Equal(HttpStatusCode.Created, registrationResponse.StatusCode);

        var registeredUser = await ReadWrappedDataAsync<RegisterUserDto>(registrationResponse);
        Assert.NotNull(registeredUser);
        Assert.NotEqual(Guid.Empty, registeredUser.UserId);
        Assert.Equal(userName, registeredUser.UserName);
        Assert.Equal(email, registeredUser.Email);

        using var duplicateRegistrationResponse = await client.PostAsJsonAsync("/api/v1/identity/users", registrationRequest);
        Assert.Equal(HttpStatusCode.Conflict, duplicateRegistrationResponse.StatusCode);

        using (var duplicateDocument = await ReadJsonDocumentAsync(duplicateRegistrationResponse))
        {
            Assert.Equal(
                "identity.registration.username_conflict",
                duplicateDocument.RootElement.GetProperty("code").GetString());
        }

        using var invalidTokenResponse = await client.PostAsJsonAsync(
            "/api/v1/identity/tokens",
            new IssueAccessTokenInput(userName, "wrong-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, invalidTokenResponse.StatusCode);

        using (var invalidTokenDocument = await ReadJsonDocumentAsync(invalidTokenResponse))
        {
            Assert.Equal(
                "identity.authentication.invalid_credentials",
                invalidTokenDocument.RootElement.GetProperty("code").GetString());
        }

        using var tokenResponse = await client.PostAsJsonAsync(
            "/api/v1/identity/tokens",
            new IssueAccessTokenInput(userName, password));

        Assert.Equal(HttpStatusCode.OK, tokenResponse.StatusCode);

        var issuedToken = await ReadWrappedDataAsync<AccessTokenDto>(tokenResponse);
        Assert.NotNull(issuedToken);
        Assert.Equal("Bearer", issuedToken.TokenType);
        Assert.False(string.IsNullOrWhiteSpace(issuedToken.AccessToken));

        using var activeIntrospectionResponse = await client.PostAsJsonAsync(
            "/api/v1/identity/tokens/introspect",
            new IntrospectAccessTokenInput(issuedToken.AccessToken));

        Assert.Equal(HttpStatusCode.OK, activeIntrospectionResponse.StatusCode);

        using (var activeDocument = await ReadJsonDocumentAsync(activeIntrospectionResponse))
        {
            var activeData = activeDocument.RootElement.GetProperty("data");
            Assert.True(activeData.GetProperty("isActive").GetBoolean());
            Assert.Equal(registeredUser.UserId.ToString(), activeData.GetProperty("subjectId").GetString());
            Assert.Contains(
                "platform",
                activeData
                    .GetProperty("scopes")
                    .EnumerateArray()
                    .Select(scope => scope.GetString())
                    .OfType<string>());

            var claims = activeData.GetProperty("claims");
            Assert.Equal(userName, claims.GetProperty("preferred_username").GetString());
            Assert.Equal(email, claims.GetProperty("email").GetString());
        }

        using var inactiveIntrospectionResponse = await client.PostAsJsonAsync(
            "/api/v1/identity/tokens/introspect",
            new IntrospectAccessTokenInput("invalid-token"));

        Assert.Equal(HttpStatusCode.OK, inactiveIntrospectionResponse.StatusCode);

        using (var inactiveDocument = await ReadJsonDocumentAsync(inactiveIntrospectionResponse))
        {
            var inactiveData = inactiveDocument.RootElement.GetProperty("data");
            Assert.False(inactiveData.GetProperty("isActive").GetBoolean());
        }
    }

    private static async Task<T?> ReadWrappedDataAsync<T>(HttpResponseMessage response)
    {
        using var document = await ReadJsonDocumentAsync(response);
        if (!document.RootElement.TryGetProperty("data", out var data))
        {
            return default;
        }

        return data.Deserialize<T>(WebJsonOptions);
    }

    private static async Task<JsonDocument> ReadJsonDocumentAsync(HttpResponseMessage response)
    {
        await using var contentStream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(contentStream);
    }
}