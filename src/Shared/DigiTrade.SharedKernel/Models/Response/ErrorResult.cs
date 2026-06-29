using System.Net;

namespace DigiTrade.SharedKernel.Models.Response;

/// <summary>
/// Error result for use case.
/// </summary>
public class ErrorResult(string error, int statusCode = (int)HttpStatusCode.BadRequest)
    : StatusResult(statusCode)
{
    public string Error { get; } = error;
}