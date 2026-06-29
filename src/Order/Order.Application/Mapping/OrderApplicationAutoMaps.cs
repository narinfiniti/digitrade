using AutoMapper;
using DigiTrade.Common.Mapping;
using Order.Application.Models;
using OrderAggregate = Order.Domain.Orders.Order;

namespace Order.Application.Mapping;

public sealed class OrderToOrderDetailsMap : IAutoMap<OrderAggregate, OrderDetailsModel>
{
    public void CreateMap(Profile profile)
    {
        profile.CreateMap<OrderAggregate, OrderDetailsModel>()
            .ForCtorParam(nameof(OrderDetailsModel.OrderId), options => options.MapFrom(source => source.Id));
    }
}