using DigiTrade.Common.Mapping;
using Risk.Application.Models;
using MarginAccountAggregate = Risk.Domain.Margins.MarginAccount;

namespace Risk.Api.Mapping;

public sealed class RiskApiMappingProfile : AssemblyScanningMappingProfile
{
    public RiskApiMappingProfile()
        : base(
            typeof(RiskApiMappingProfile).Assembly,
            typeof(MarginAccountDetailsModel).Assembly,
            typeof(MarginAccountAggregate).Assembly)
    {
    }
}