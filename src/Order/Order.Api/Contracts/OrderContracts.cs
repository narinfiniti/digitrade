using AutoMapper;
using DigiTrade.Common.Mapping;
using Order.Application.Models;
using Order.Domain.Orders;

namespace Order.Api.Contracts;

public sealed record PlaceOrderInput(
    string AccountId,
    string InstrumentId,
    OrderDirection Direction,
    decimal Quantity,
    decimal RequestedPrice);

public sealed record OrderDto(
    Guid OrderId,
    string AccountId,
    string InstrumentId,
    OrderDirection Direction,
    OrderStatus Status,
    decimal Quantity,
    decimal RequestedPrice,
    DateTimeOffset SubmittedAtUtc,
    DateTimeOffset? AcceptedAtUtc,
    DateTimeOffset? RejectedAtUtc,
    DateTimeOffset? CancelledAtUtc,
    int Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt) : IAutoMap<OrderDetailsModel, OrderDto>
{
    public void CreateMap(Profile profile)
    {
        profile.CreateMap<OrderDetailsModel, OrderDto>();
    }
}