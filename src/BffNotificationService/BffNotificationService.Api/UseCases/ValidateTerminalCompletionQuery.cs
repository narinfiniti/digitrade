using BffNotificationService.Api.Contracts;
using DigiTrade.SharedKernel.Abstractions;
using MediatR;

namespace BffNotificationService.Api.UseCases;

public sealed class ValidateTerminalCompletionQuery(ValidateTerminalCompletionQuery.Model? input)
    : IUseCase<ValidateTerminalCompletionQuery.Model, Dictionary<string, string[]>>
{
    public Model? Input => input;

    public sealed record Model(TerminalNotificationInput Request);

    public sealed class Handler : IRequestHandler<ValidateTerminalCompletionQuery, Dictionary<string, string[]>>
    {
        public Task<Dictionary<string, string[]>> Handle(ValidateTerminalCompletionQuery request, CancellationToken cancellationToken)
        {
            var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
            var input = request.Input?.Request;
            if (input is null)
            {
                errors[nameof(TerminalNotificationInput.AggregateId)] = ["'Aggregate Id' must not be empty."];
                return Task.FromResult(errors);
            }

            AddErrorIfMissing(errors, nameof(TerminalNotificationInput.AggregateId), input.AggregateId, "'Aggregate Id' must not be empty.");
            AddErrorIfMissing(errors, nameof(TerminalNotificationInput.RecipientId), input.RecipientId, "'Recipient Id' must not be empty.");
            AddErrorIfMissing(errors, nameof(TerminalNotificationInput.Channel), input.Channel, "'Channel' must not be empty.");
            AddErrorIfMissing(errors, nameof(TerminalNotificationInput.Subject), input.Subject, "'Subject' must not be empty.");
            AddErrorIfMissing(errors, nameof(TerminalNotificationInput.Message), input.Message, "'Message' must not be empty.");

            return Task.FromResult(errors);
        }

        private static void AddErrorIfMissing(
            Dictionary<string, string[]> errors,
            string propertyName,
            string propertyValue,
            string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(propertyValue))
            {
                errors[propertyName] = [errorMessage];
            }
        }
    }
}