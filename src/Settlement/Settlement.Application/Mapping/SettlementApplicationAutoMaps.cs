using AutoMapper;
using DigiTrade.Common.Mapping;
using Settlement.Application.Models;
using SettlementAggregate = Settlement.Domain.Settlements.Settlement;

namespace Settlement.Application.Mapping;

public sealed class SettlementToSettlementDetailsMap : IAutoMap<SettlementAggregate, SettlementDetailsModel>
{
    public void CreateMap(Profile profile)
    {
        profile.CreateMap<SettlementAggregate, SettlementDetailsModel>()
            .ForCtorParam(nameof(SettlementDetailsModel.SettlementId), options => options.MapFrom(source => source.Id));
    }
}