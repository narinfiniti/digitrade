using System.Net;

namespace DigiTrade.Common.Exceptions;

/// <summary>
/// The requested content only acceptable according to the Accept headers.
/// </summary>
public class NotAcceptableException(string message = "Content type is not acceptable.")
    : StatusCodeException(message, (int)HttpStatusCode.NotAcceptable);