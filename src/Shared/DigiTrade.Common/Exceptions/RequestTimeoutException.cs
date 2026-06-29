using System.Net;

namespace DigiTrade.Common.Exceptions;

/// <summary>
/// The server timed out waiting for the request exception. 
/// </summary>
public class RequestTimeoutException(string message = "The server timed out waiting for the request.")
    : StatusCodeException(message, (int)HttpStatusCode.RequestTimeout);