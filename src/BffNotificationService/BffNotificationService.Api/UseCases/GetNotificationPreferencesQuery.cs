using BffNotificationService.Api.Contracts;
using DigiTrade.SharedKernel.Abstractions;
using MediatR;

namespace BffNotificationService.Api.UseCases;

public sealed class GetNotificationPreferencesQuery(GetNotificationPreferencesQuery.Model? input)
    : IUseCase<GetNotificationPreferencesQuery.Model, NotificationPreferenceDto>
{
    public Model? Input => input;

    public sealed record Model(string UserId);

    public sealed class Handler : IRequestHandler<GetNotificationPreferencesQuery, NotificationPreferenceDto>
    {
        public Task<NotificationPreferenceDto> Handle(GetNotificationPreferencesQuery request, CancellationToken cancellationToken)
        {
            var userId = string.IsNullOrWhiteSpace(request.Input?.UserId) ? "anonymous-subject" : request.Input.UserId;
            return Task.FromResult(NotificationContractState.PreferencesByUser.GetOrAdd(userId, NotificationContractState.CreateDefaultPreferences));
        }
    }
}