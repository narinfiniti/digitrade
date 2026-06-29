using AutoMapper;
using BffOrchestratorService.Domain.Models;
using DigiTrade.Common.Mapping;

namespace BffOrchestratorService.Application.Contracts;

public sealed record OrchestrationDependencyStatusDto(
    string ServiceName,
    bool IsHealthy,
    int StatusCode,
    string Endpoint,
    string? FailureReason)
    : IAutoMap<OrchestrationDependencyStatusModel, OrchestrationDependencyStatusDto>
{
    public void CreateMap(Profile profile)
    {
        profile.CreateMap<OrchestrationDependencyStatusModel, OrchestrationDependencyStatusDto>();
    }
}