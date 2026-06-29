using System.Net;

namespace DigiTrade.Common.Exceptions;

/// <summary>
/// The requested Resource is no longer available.
/// </summary>
public class GoneException(
    string message = "Resource is no longer available.",
    int statusCode = (int)HttpStatusCode.Gone)
    : StatusCodeException(message, statusCode);