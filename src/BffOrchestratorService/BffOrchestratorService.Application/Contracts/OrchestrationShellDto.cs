using AutoMapper;
using BffOrchestratorService.Domain.Entities;
using DigiTrade.Common.Mapping;

namespace BffOrchestratorService.Application.Contracts;

public sealed record OrchestrationShellDto(
    Guid OrchestrationShellId,
    string FlowName,
    string CorrelationId,
    string RequestedBySubjectId,
    string RequestedByUserName,
    string Status,
    bool DependenciesHealthy,
    int Version,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyCollection<OrchestrationDependencyStatusDto> Dependencies)
    : IAutoMap<OrchestrationShell, OrchestrationShellDto>
{
    public void CreateMap(Profile profile)
    {
        profile.CreateMap<OrchestrationShell, OrchestrationShellDto>()
            .ForCtorParam(nameof(OrchestrationShellDto.OrchestrationShellId), options => options.MapFrom(source => source.Id))
            .ForCtorParam(nameof(OrchestrationShellDto.CreatedAtUtc), options => options.MapFrom(source => source.CreatedAt))
            .ForCtorParam(nameof(OrchestrationShellDto.UpdatedAtUtc), options => options.MapFrom(source => source.UpdatedAt));
    }
}