using System.Net;

namespace DigiTrade.Common.Exceptions;

/// <summary>
/// Operation could not be processed due to a precondition failure exception.
/// </summary>
public class PreconditionFailedException(
    string message = "Operation could not be processed due to precondition failure.")
    : StatusCodeException(message, (int)HttpStatusCode.PreconditionFailed);