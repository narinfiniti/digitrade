using BffNotificationService.Api.Contracts;
using DigiTrade.SharedKernel.Abstractions;
using MediatR;

namespace BffNotificationService.Api.UseCases;

public sealed class GetNotificationByIdQuery(GetNotificationByIdQuery.Model? input)
    : IUseCase<GetNotificationByIdQuery.Model, NotificationHistoryItemDto?>
{
    public Model? Input => input;

    public sealed record Model(string UserId, Guid NotificationId);

    public sealed class Handler : IRequestHandler<GetNotificationByIdQuery, NotificationHistoryItemDto?>
    {
        public Task<NotificationHistoryItemDto?> Handle(GetNotificationByIdQuery request, CancellationToken cancellationToken)
        {
            var input = request.Input;
            if (input is null || input.NotificationId == Guid.Empty)
            {
                return Task.FromResult<NotificationHistoryItemDto?>(null);
            }

            var item = NotificationContractState.CreateDefaultHistory(input.UserId, false)
                .FirstOrDefault(entry => entry.NotificationId == input.NotificationId);
            return Task.FromResult(item);
        }
    }
}