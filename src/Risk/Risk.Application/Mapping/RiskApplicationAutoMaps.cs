using AutoMapper;
using DigiTrade.Common.Mapping;
using Risk.Application.Models;
using MarginAccountAggregate = Risk.Domain.Margins.MarginAccount;

namespace Risk.Application.Mapping;

public sealed class MarginAccountToDetailsMap : IAutoMap<MarginAccountAggregate, MarginAccountDetailsModel>
{
    public void CreateMap(Profile profile)
    {
        profile.CreateMap<MarginAccountAggregate, MarginAccountDetailsModel>()
            .ForCtorParam(nameof(MarginAccountDetailsModel.MarginAccountId), options => options.MapFrom(source => source.Id));
    }
}