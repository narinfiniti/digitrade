using AutoMapper;
using BffAggregatorService.Application.Models;
using DigiTrade.Common.Mapping;

namespace BffAggregatorService.Application.Contracts;

public sealed record ServiceHealthSummaryDto(
    bool IsHealthy,
    IReadOnlyCollection<DownstreamServiceHealthDto> Services) : IAutoMap<ServiceHealthSummaryModel, ServiceHealthSummaryDto>
{
    public void CreateMap(Profile profile)
    {
        profile.CreateMap<ServiceHealthSummaryModel, ServiceHealthSummaryDto>();
    }
}