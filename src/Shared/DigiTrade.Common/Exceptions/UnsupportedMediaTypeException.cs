using System.Net;

namespace DigiTrade.Common.Exceptions;

/// <summary>
/// Request resource media type does not supported.
/// </summary>
public class UnsupportedMediaTypeException(string message = "Request resource media type does not supported.")
    : StatusCodeException(message, (int)HttpStatusCode.UnsupportedMediaType);