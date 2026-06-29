using System.Net;

namespace DigiTrade.Common.Exceptions;

/// <summary>
/// Necessary permissions exception.
/// </summary>
public class ForbiddenException : StatusCodeException
{
    public ForbiddenException(
        string message = "Do not have permissions for the operation."
    ) : base(message, (int)HttpStatusCode.Forbidden) { }
    public ForbiddenException(
        string message = "Do not have permissions for the operation.",
        Exception? innerException = null
    ) : base(message, innerException, (int)HttpStatusCode.Forbidden) { }

}