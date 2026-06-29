using BffNotificationService.Api.Contracts;
using DigiTrade.SharedKernel.Abstractions;
using MediatR;

namespace BffNotificationService.Api.UseCases;

public sealed class GetUnreadNotificationsQuery(GetUnreadNotificationsQuery.Model? input)
    : IUseCase<GetUnreadNotificationsQuery.Model, NotificationHistoryDto>
{
    public Model? Input => input;

    public sealed record Model(string UserId);

    public sealed class Handler : IRequestHandler<GetUnreadNotificationsQuery, NotificationHistoryDto>
    {
        public Task<NotificationHistoryDto> Handle(GetUnreadNotificationsQuery request, CancellationToken cancellationToken)
        {
            var userId = string.IsNullOrWhiteSpace(request.Input?.UserId) ? "anonymous-subject" : request.Input.UserId;
            var items = NotificationContractState.CreateDefaultHistory(userId, true);
            return Task.FromResult(new NotificationHistoryDto(userId, items.Length, items.Length, items));
        }
    }
}