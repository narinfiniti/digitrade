using System.Net;

namespace DigiTrade.Common.Exceptions;

/// <summary>
/// The server unable to fulfil the request exception.
/// </summary>
public class NotImplementedException(string message = "The server unable to fulfill the request.")
    : StatusCodeException(message, (int)HttpStatusCode.NotImplemented);