using System.Net;

namespace DigiTrade.Common.Exceptions;

/// <summary>
/// Necessary permissions exception.
/// </summary>
public class MethodNotAllowedException(string message = "A request method is not supported.")
    : StatusCodeException(message, (int)HttpStatusCode.MethodNotAllowed);