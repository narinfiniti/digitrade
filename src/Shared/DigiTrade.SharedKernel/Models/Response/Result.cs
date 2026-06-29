using System.Text.Json;
using DigiTrade.Common.JsonOptions;

namespace DigiTrade.SharedKernel.Models.Response;

/// <summary>
/// Use case result with data.
/// </summary>
public class Result
{
    private sealed class Body<T>
    {
        public T? Data { get; }
        public string? Error { get; }
        public int Status { get; }
        
        internal Body(T? data, string? error = default, int? status = default)
        {
            Data = data;
            Status = status ?? (string.IsNullOrEmpty(error) ? 200 : 500);
            Error = error;
        }
    }

    public static string Ok<TOk>(TOk data, int status = 200)
    {
        var ops = JsonSettingProvider.JsonSerializerOps;
        var result = new Body<TOk>(
            status == 204 ? default : data,
            status: status);
        return JsonSerializer.Serialize(result, ops);
    }
    public static string Error<TEr>(TEr data, int status = 500)
    {
        var ops = JsonSettingProvider.JsonSerializerOps;
        var result = new Body<TEr>(
            default,
            error: data as string ?? JsonSerializer.Serialize(data, ops),
            status: status);
        return JsonSerializer.Serialize(result, ops);
    }
}