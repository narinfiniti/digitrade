using DigiTrade.Common.Mapping;
using Settlement.Application.Models;
using SettlementAggregate = Settlement.Domain.Settlements.Settlement;

namespace Settlement.Api.Mapping;

public sealed class SettlementApiMappingProfile : AssemblyScanningMappingProfile
{
    public SettlementApiMappingProfile()
        : base(
            typeof(SettlementApiMappingProfile).Assembly,
            typeof(SettlementDetailsModel).Assembly,
            typeof(SettlementAggregate).Assembly)
    {
    }
}