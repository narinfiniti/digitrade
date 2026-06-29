using System.Net;

namespace DigiTrade.SharedKernel.Models.Response;

/// <summary>
/// Use case result with data.
/// </summary>
public class DataResult<TData>(
    TData data,
    int status = (int)HttpStatusCode.OK) : StatusResult(status)
{
    public TData Data { get; } = data;
}