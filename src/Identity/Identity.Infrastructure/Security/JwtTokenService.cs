using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DigiTrade.Security.Contracts;
using Identity.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Identity.Infrastructure.Security;

public sealed class JwtTokenService(IOptions<IdentityTokenOptions> optionsAccessor, TimeProvider timeProvider) : ITokenIssuer, ITokenIntrospectionService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public Task<IssuedAccessToken> IssueAsync(AccessTokenDescriptor descriptor, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        var now = timeProvider.GetUtcNow();
        var options = optionsAccessor.Value;
        var payload = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["sub"] = descriptor.SubjectId,
            ["preferred_username"] = descriptor.UserName,
            ["scope"] = string.Join(' ', descriptor.Scopes),
            ["exp"] = descriptor.ExpiresAtUtc.ToUnixTimeSeconds(),
            ["iat"] = now.ToUnixTimeSeconds(),
            ["nbf"] = now.ToUnixTimeSeconds(),
            ["iss"] = options.Issuer,
            ["aud"] = options.Audience,
        };

        foreach (var claim in descriptor.Claims)
        {
            payload[claim.Key] = claim.Value;
        }

        var headerSegment = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, string>
        {
            ["alg"] = "HS256",
            ["typ"] = "JWT",
        }, SerializerOptions));

        var payloadSegment = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(payload, SerializerOptions));
        var signingInput = $"{headerSegment}.{payloadSegment}";
        var signatureSegment = Base64UrlEncode(ComputeSignature(signingInput, options.SigningKey));

        return Task.FromResult(new IssuedAccessToken($"{signingInput}.{signatureSegment}", descriptor.ExpiresAtUtc));
    }

    public Task<TokenIntrospectionResult> IntrospectAsync(string token, CancellationToken cancellationToken = default)
    {
        if (!TryValidate(token, out var subjectId, out var expiresAtUtc, out var scopes, out var claims))
        {
            return Task.FromResult(new TokenIntrospectionResult(false, null, null, Array.Empty<string>(), new Dictionary<string, string>(StringComparer.Ordinal)));
        }

        return Task.FromResult(new TokenIntrospectionResult(true, subjectId, expiresAtUtc, scopes, claims));
    }

    private bool TryValidate(
        string token,
        out string? subjectId,
        out DateTimeOffset? expiresAtUtc,
        out IReadOnlyCollection<string> scopes,
        out IReadOnlyDictionary<string, string> claims)
    {
        subjectId = null;
        expiresAtUtc = null;
        scopes = Array.Empty<string>();
        claims = new Dictionary<string, string>(StringComparer.Ordinal);

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var segments = token.Split('.', StringSplitOptions.None);
        if (segments.Length != 3)
        {
            return false;
        }

        JsonDocument headerDocument;
        JsonDocument payloadDocument;

        try
        {
            headerDocument = JsonDocument.Parse(Base64UrlDecode(segments[0]));
            payloadDocument = JsonDocument.Parse(Base64UrlDecode(segments[1]));
        }
        catch (Exception) when (token is not null)
        {
            return false;
        }

        using (headerDocument)
        using (payloadDocument)
        {
            if (!headerDocument.RootElement.TryGetProperty("alg", out var algorithmElement)
                || !string.Equals(algorithmElement.GetString(), "HS256", StringComparison.Ordinal))
            {
                return false;
            }

            byte[] providedSignature;
            try
            {
                providedSignature = Base64UrlDecode(segments[2]);
            }
            catch (FormatException)
            {
                return false;
            }

            var options = optionsAccessor.Value;
            var expectedSignature = ComputeSignature($"{segments[0]}.{segments[1]}", options.SigningKey);
            if (!CryptographicOperations.FixedTimeEquals(expectedSignature, providedSignature))
            {
                return false;
            }

            var payload = payloadDocument.RootElement;
            if (!TryGetString(payload, "iss", out var issuer)
                || !string.Equals(issuer, options.Issuer, StringComparison.Ordinal)
                || !TryValidateAudience(payload, options.Audience)
                || !TryGetInt64(payload, "exp", out var expUnixSeconds))
            {
                return false;
            }

            var now = timeProvider.GetUtcNow().ToUnixTimeSeconds();
            if (expUnixSeconds <= now)
            {
                return false;
            }

            if (TryGetInt64(payload, "nbf", out var nbfUnixSeconds) && nbfUnixSeconds > now)
            {
                return false;
            }

            expiresAtUtc = DateTimeOffset.FromUnixTimeSeconds(expUnixSeconds);
            TryGetString(payload, "sub", out subjectId);

            var scopeValue = TryGetString(payload, "scope", out var scopeText)
                ? scopeText
                : string.Empty;
            scopes = string.IsNullOrWhiteSpace(scopeValue)
                ? Array.Empty<string>()
                : scopeValue.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            claims = ExtractClaims(payload);
            return true;
        }
    }

    private static Dictionary<string, string> ExtractClaims(JsonElement payload)
    {
        var claims = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var property in payload.EnumerateObject())
        {
            switch (property.Value.ValueKind)
            {
                case JsonValueKind.String:
                    claims[property.Name] = property.Value.GetString() ?? string.Empty;
                    break;
                case JsonValueKind.Number:
                    claims[property.Name] = property.Value.GetRawText();
                    break;
                case JsonValueKind.True:
                case JsonValueKind.False:
                    claims[property.Name] = property.Value.GetBoolean().ToString();
                    break;
            }
        }

        return claims;
    }

    private static bool TryValidateAudience(JsonElement payload, string expectedAudience)
    {
        if (!payload.TryGetProperty("aud", out var audienceElement))
        {
            return false;
        }

        return audienceElement.ValueKind switch
        {
            JsonValueKind.String => string.Equals(audienceElement.GetString(), expectedAudience, StringComparison.Ordinal),
            JsonValueKind.Array => audienceElement.EnumerateArray().Any(item => string.Equals(item.GetString(), expectedAudience, StringComparison.Ordinal)),
            _ => false,
        };
    }

    private static bool TryGetString(JsonElement payload, string propertyName, out string? value)
    {
        value = null;
        return payload.TryGetProperty(propertyName, out var property)
            && property.ValueKind == JsonValueKind.String
            && (value = property.GetString()) is not null;
    }

    private static bool TryGetInt64(JsonElement payload, string propertyName, out long value)
    {
        value = default;
        return payload.TryGetProperty(propertyName, out var property)
            && property.ValueKind == JsonValueKind.Number
            && property.TryGetInt64(out value);
    }

    private static byte[] ComputeSignature(string signingInput, string signingKey)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(signingKey));
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(signingInput));
    }

    private static string Base64UrlEncode(byte[] value)
    {
        return Convert.ToBase64String(value)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded += (padded.Length % 4) switch
        {
            2 => "==",
            3 => "=",
            _ => string.Empty,
        };

        return Convert.FromBase64String(padded);
    }
}