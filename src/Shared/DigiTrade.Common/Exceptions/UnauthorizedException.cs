using System.Net;

namespace DigiTrade.Common.Exceptions;

/// <summary>
/// Invalid authentication credentials exception.
/// </summary>
public class UnauthorizedException(
    string message = "Invalid authentication credentials for the target resource.",
    int statusCode = (int)HttpStatusCode.Unauthorized)
    : StatusCodeException(message, statusCode);