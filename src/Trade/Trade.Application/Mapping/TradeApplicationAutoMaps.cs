using AutoMapper;
using DigiTrade.Common.Mapping;
using Trade.Application.Models;
using TradeAggregate = Trade.Domain.Trades.Trade;

namespace Trade.Application.Mapping;

public sealed class TradeToTradeDetailsMap : IAutoMap<TradeAggregate, TradeDetailsModel>
{
    public void CreateMap(Profile profile)
    {
        profile.CreateMap<TradeAggregate, TradeDetailsModel>()
            .ForCtorParam(nameof(TradeDetailsModel.TradeId), options => options.MapFrom(source => source.Id));
    }
}