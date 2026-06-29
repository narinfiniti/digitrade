using DigiTrade.SharedKernel.Models.Response;
using Microsoft.AspNetCore.Http;

namespace DigiTrade.SharedKernel.Filters;

/// <summary>
/// Normalizes minimal API result payloads into shared response envelopes.
/// </summary>
public sealed class ResponseResultFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var endpointResult = await next(context);
        return WrapEndpointResult(endpointResult);
    }

    private static object? WrapEndpointResult(object? endpointResult)
    {
        if (endpointResult is null)
        {
            return null;
        }

        if (endpointResult is IResult result)
        {
            return WrapIResult(result);
        }

        if (IsWrappedValue(endpointResult))
        {
            return endpointResult;
        }

        return Results.Ok(new DataResult<object?>(endpointResult));
    }

    private static IResult WrapIResult(IResult result)
    {
        if (result is not IValueHttpResult valueHttpResult)
        {
            return result;
        }

        var value = valueHttpResult.Value;
        if (value is null || IsWrappedValue(value))
        {
            return result;
        }

        var statusCode = (result as IStatusCodeHttpResult)?.StatusCode ?? StatusCodes.Status200OK;
        if (statusCode is StatusCodes.Status204NoContent or StatusCodes.Status304NotModified)
        {
            return result;
        }

        if (statusCode == StatusCodes.Status400BadRequest)
        {
            var message = value is HttpValidationProblemDetails details
                ? GetValidationErrorMessage(details)
                : value.ToString() ?? "One or more validation failures have occurred.";

            var error = new ErrorResult(message, StatusCodes.Status422UnprocessableEntity);
            return Results.Json(error, statusCode: error.Status);
        }

        if (statusCode >= StatusCodes.Status400BadRequest)
        {
            return result;
        }

        var wrappedData = new DataResult<object?>(value, statusCode);

        if (statusCode == StatusCodes.Status201Created && TryGetLocation(result, out var createdLocation))
        {
            return Results.Created(createdLocation, wrappedData);
        }

        if (statusCode == StatusCodes.Status202Accepted && TryGetLocation(result, out var acceptedLocation))
        {
            return Results.Accepted(acceptedLocation, wrappedData);
        }

        return Results.Json(wrappedData, statusCode: statusCode);
    }

    private static bool IsWrappedValue(object value)
    {
        if (value is StatusResult)
        {
            return true;
        }

        var valueType = value.GetType();
        return valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(DataResult<>);
    }

    private static bool TryGetLocation(IResult result, out string location)
    {
        var locationProperty = result.GetType().GetProperty("Location");
        if (locationProperty?.PropertyType == typeof(string)
            && locationProperty.GetValue(result) is string locationValue
            && !string.IsNullOrWhiteSpace(locationValue))
        {
            location = locationValue;
            return true;
        }

        location = string.Empty;
        return false;
    }

    private static string GetValidationErrorMessage(HttpValidationProblemDetails details)
    {
        foreach (var messages in details.Errors.Values)
        {
            var firstMessage = messages.FirstOrDefault(static message => !string.IsNullOrWhiteSpace(message));
            if (!string.IsNullOrWhiteSpace(firstMessage))
            {
                return firstMessage;
            }
        }

        return "One or more validation failures have occurred.";
    }
}