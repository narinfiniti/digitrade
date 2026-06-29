using System.Net;

namespace DigiTrade.Common.Exceptions;

/// <summary>
/// Request context was not found exception.
/// </summary>
public class NotFoundException(
    string? message = null,
    int statusCode = (int)HttpStatusCode.NotFound)
    : StatusCodeException(message, statusCode)
{
    public NotFoundException() : this("The requested resource was not found.") { }
    public NotFoundException(Type type) : this($"Entity '{type.Name}' was not found.") { }
    public NotFoundException(string name, object key) : this($"Entity \"{name}\" ({key}) was not found.") { }
}