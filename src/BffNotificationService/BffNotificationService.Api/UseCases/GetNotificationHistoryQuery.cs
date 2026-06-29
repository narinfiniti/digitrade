using BffNotificationService.Api.Contracts;
using DigiTrade.SharedKernel.Abstractions;
using MediatR;

namespace BffNotificationService.Api.UseCases;

public sealed class GetNotificationHistoryQuery(GetNotificationHistoryQuery.Model? input)
    : IUseCase<GetNotificationHistoryQuery.Model, NotificationHistoryDto>
{
    public Model? Input => input;

    public sealed record Model(string UserId);

    public sealed class Handler : IRequestHandler<GetNotificationHistoryQuery, NotificationHistoryDto>
    {
        public Task<NotificationHistoryDto> Handle(GetNotificationHistoryQuery request, CancellationToken cancellationToken)
        {
            var userId = string.IsNullOrWhiteSpace(request.Input?.UserId) ? "anonymous-subject" : request.Input.UserId;
            var items = NotificationContractState.CreateDefaultHistory(userId, false);
            return Task.FromResult(new NotificationHistoryDto(userId, items.Length, items.Count(item => !item.IsRead), items));
        }
    }
}