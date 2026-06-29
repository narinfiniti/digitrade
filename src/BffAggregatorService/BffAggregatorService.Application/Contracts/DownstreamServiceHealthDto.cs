using AutoMapper;
using BffAggregatorService.Application.Models;
using DigiTrade.Common.Mapping;

namespace BffAggregatorService.Application.Contracts;

public sealed record DownstreamServiceHealthDto(
    string ServiceName,
    bool IsHealthy,
    int StatusCode,
    string Endpoint,
    string? FailureReason) : IAutoMap<DownstreamServiceHealthModel, DownstreamServiceHealthDto>
    {
        public void CreateMap(Profile profile)
        {
            profile.CreateMap<DownstreamServiceHealthModel, DownstreamServiceHealthDto>();
        }
    }