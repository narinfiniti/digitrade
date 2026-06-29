using System.Net;

namespace DigiTrade.Common.Exceptions;

/// <summary>
/// Operation could not be processed due to a conflict exception.
/// </summary>
public class ConflictException(string message = "Operation could not be processed due to a conflict.")
    : StatusCodeException(message, (int)HttpStatusCode.Conflict);