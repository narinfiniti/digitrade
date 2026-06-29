using AutoMapper;
using BffNotificationService.Api.Contracts;
using BffNotificationService.Domain.Notifications;
using DigiTrade.SharedKernel.Abstractions;
using MediatR;

namespace BffNotificationService.Api.UseCases;

public sealed class GetNotificationDeliveryQuery(GetNotificationDeliveryQuery.Model? input)
    : IUseCase<GetNotificationDeliveryQuery.Model, NotificationDeliveryDto?>
{
    public Model? Input => input;

    public sealed record Model(Guid NotificationDeliveryId);

    public sealed class Handler(
        IMapper mapper,
        INotificationDeliveryStore notificationDeliveryStore)
        : IRequestHandler<GetNotificationDeliveryQuery, NotificationDeliveryDto?>
    {
        public async Task<NotificationDeliveryDto?> Handle(GetNotificationDeliveryQuery request, CancellationToken cancellationToken)
        {
            var input = request.Input;
            if (input is null || input.NotificationDeliveryId == Guid.Empty)
            {
                return null;
            }

            var notificationDelivery = await notificationDeliveryStore.GetAsync(input.NotificationDeliveryId, cancellationToken);
            return notificationDelivery is null ? null : mapper.Map<NotificationDeliveryDto>(notificationDelivery);
        }
    }
}