using System.Net;

namespace DigiTrade.Common.Exceptions;

/// <summary>
/// The server cannot handle the request exception.
/// </summary>
public class ServiceUnavailableException(string message = "Requested resource cannot be served.")
    : StatusCodeException(message, (int)HttpStatusCode.ServiceUnavailable);