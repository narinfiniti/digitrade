using AutoMapper;
using BffNotificationService.Domain.Notifications;
using DigiTrade.Common.Mapping;

namespace BffNotificationService.Api.Contracts;

public sealed record NotificationDeliveryDto(
    Guid NotificationDeliveryId,
    Guid EventId,
    string AggregateId,
    string RecipientId,
    string Channel,
    string Subject,
    string Message,
    string CorrelationId,
    string DeliveryProvider,
    string DeliveryStatus,
    DateTimeOffset DeliveredAtUtc,
    int Version,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc) : IAutoMap<NotificationDelivery, NotificationDeliveryDto>
{
    public void CreateMap(Profile profile)
    {
        profile.CreateMap<NotificationDelivery, NotificationDeliveryDto>()
            .ForCtorParam(nameof(NotificationDeliveryDto.NotificationDeliveryId),
                options => options.MapFrom(source => source.Id))
            .ForCtorParam(nameof(NotificationDeliveryDto.CreatedAtUtc),
                options => options.MapFrom(source => source.CreatedAt))
            .ForCtorParam(nameof(NotificationDeliveryDto.UpdatedAtUtc),
                options => options.MapFrom(source => source.UpdatedAt));
    }
}