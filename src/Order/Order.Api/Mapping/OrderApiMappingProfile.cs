using DigiTrade.Common.Mapping;
using Order.Application.Models;
using OrderAggregate = Order.Domain.Orders.Order;

namespace Order.Api.Mapping;

public sealed class OrderApiMappingProfile : AssemblyScanningMappingProfile
{
    public OrderApiMappingProfile()
        : base(
            typeof(OrderApiMappingProfile).Assembly,
            typeof(OrderDetailsModel).Assembly,
            typeof(OrderAggregate).Assembly)
    {
    }
}