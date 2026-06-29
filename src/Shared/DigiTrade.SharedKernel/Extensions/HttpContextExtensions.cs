using Microsoft.AspNetCore.Http;

namespace DigiTrade.SharedKernel.Extensions;

public static class HttpContextExtensions
{
    public static string GetCorrelationId(this HttpContext httpContext)
    {
        return httpContext.Request.Headers.TryGetValue("X-Correlation-Id", out var values)
               && !string.IsNullOrWhiteSpace(values.ToString())
            ? values.ToString()
            : httpContext.TraceIdentifier;
    }
    
    public const string CorrelationHeaderName = "X-Correlation-Id";
    public const string IdempotencyHeaderName = "Idempotency-Key";
    public const string AuthenticatedSubjectHeaderName = "X-Authenticated-Subject";
    public const string AuthenticatedUserNameHeaderName = "X-Authenticated-UserName";

    public static string GetHeaderOrDefault(this HttpContext httpContext, string headerName, string fallbackValue)
    {
        return httpContext.Request.Headers.TryGetValue(headerName, out var values)
               && !string.IsNullOrWhiteSpace(values.ToString())
            ? values.ToString()
            : fallbackValue;
    }
}