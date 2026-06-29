using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Runtime.Serialization;
using System.Text.Json;
using DigiTrade.Common.Models;

namespace DigiTrade.Common.Exceptions;

/// <summary>
/// Request context validation exception.
/// </summary>
[DataContract]
public class ValidationException(
    string message = "One or more validation failures have occurred.",
    int statusCode = (int)HttpStatusCode.UnprocessableEntity)
    : StatusCodeException(message, statusCode)
{
    [DataMember]
    public IDictionary<string, string[]> Failures { get; } = new Dictionary<string, string[]>();

    public ValidationException(
        IEnumerable<ValidationFailure>? failures = null,
        string message = "One or more validation failures have occurred.") : this(message)
    {
        if(failures == null) return;

        var failureGroups = failures.GroupBy(static e => e.PropertyName, static e => e.ErrorMessage);
        foreach (var failureGroup in failureGroups)
        {
            var propertyName = failureGroup.Key;
            var propertyFailures = failureGroup.ToArray();

            Failures.Add(propertyName, propertyFailures);
        }
    }

    public ValidationException(
        IEnumerable<ValidationResult>? results = null,
        string message = "One or more validation failures have occurred.") : this(message)
    {
        if(results == null) return;

        var failures = results.Select(static validationResult =>
        {
            var names = string.Join(", ", validationResult.MemberNames);
            return new ValidationFailure(names, validationResult.ErrorMessage ?? string.Empty);
        });
        var failureGroups = failures.GroupBy(static e => e.PropertyName, static e => e.ErrorMessage);
        foreach (var failureGroup in failureGroups)
        {
            var propertyName = failureGroup.Key;
            var propertyFailures = failureGroup.ToArray();

            Failures.Add(propertyName, propertyFailures);
        }
    }

    public ValidationException(
        IReadOnlyCollection<KeyValuePair<string, dynamic>> modelState,
        string message = "One or more validation failures have occurred.") : this(message)
    {
        foreach (var state in modelState)
        {
            var propertyName = state.Key;
            var propertyFailures = ((IEnumerable<dynamic>)state.Value.Errors)
                .Select(static err => (string)err.ErrorMessage).ToArray();

            Failures.Add(propertyName, propertyFailures);
        }
    }

    public virtual string ToResult(JsonSerializerOptions serializerOptions)
    {
        return Failures.Any()
            ? JsonSerializer.Serialize(Failures, serializerOptions)
            : Message;
    }
}