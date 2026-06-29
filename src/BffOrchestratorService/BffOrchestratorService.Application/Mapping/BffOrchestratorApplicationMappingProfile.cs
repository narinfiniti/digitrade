using DigiTrade.Common.Mapping;

namespace BffOrchestratorService.Application.Mapping;

public sealed class BffOrchestratorApplicationMappingProfile()
    : AssemblyScanningMappingProfile(typeof(BffOrchestratorApplicationMappingProfile).Assembly);
