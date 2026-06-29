using DigiTrade.Common.Mapping;
using Trade.Application.Models;
using TradeAggregate = Trade.Domain.Trades.Trade;

namespace Trade.Api.Mapping;

public sealed class TradeApiMappingProfile : AssemblyScanningMappingProfile
{
    public TradeApiMappingProfile()
        : base(
            typeof(TradeApiMappingProfile).Assembly,
            typeof(TradeDetailsModel).Assembly,
            typeof(TradeAggregate).Assembly)
    {
    }
}