using System.Net;

namespace DigiTrade.Common.Exceptions;

/// <summary>
/// Exception with StatusCode result.
/// </summary>
public class StatusCodeException(
    string? message,
    Exception? innerException = null,
    int status = (int)HttpStatusCode.BadRequest)
    : Exception(message, innerException)
{
    public int Status { get; private set; } = status;

    public StatusCodeException(
        string? message = null,
        int statusCode = (int)HttpStatusCode.BadRequest
    ) : this(message, null, statusCode) { }
}