using System.Net;

namespace DigiTrade.Common.Exceptions;

/// <summary>
/// Necessary permissions exception.
/// </summary>
public class BadRequestException(
    string message = "The server cannot process the request due to client error/s.",
    int statusCode = (int)HttpStatusCode.BadRequest)
    : StatusCodeException(message, statusCode);