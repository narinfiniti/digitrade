using BffNotificationService.Api.Contracts;
using DigiTrade.SharedKernel.Abstractions;
using MediatR;

namespace BffNotificationService.Api.UseCases;

public sealed class PutNotificationPreferencesCommand(PutNotificationPreferencesCommand.Model? input)
    : IUseCase<PutNotificationPreferencesCommand.Model, NotificationPreferenceDto>
{
    public Model? Input => input;

    public sealed record Model(UpdateNotificationPreferenceInput Request);

    public sealed class Handler : IRequestHandler<PutNotificationPreferencesCommand, NotificationPreferenceDto>
    {
        public Task<NotificationPreferenceDto> Handle(PutNotificationPreferencesCommand request, CancellationToken cancellationToken)
        {
            var input = request.Input?.Request ?? throw new ArgumentNullException(nameof(request));
            var response = new NotificationPreferenceDto(
                input.UserId,
                input.EmailEnabled,
                input.PushEnabled,
                input.WebSocketEnabled,
                input.Categories.Distinct(StringComparer.Ordinal).ToArray());

            NotificationContractState.PreferencesByUser[input.UserId] = response;
            return Task.FromResult(response);
        }
    }
}