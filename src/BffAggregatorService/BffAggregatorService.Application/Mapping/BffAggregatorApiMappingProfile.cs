using DigiTrade.Common.Mapping;

namespace BffAggregatorService.Application.Mapping;

public sealed class BffAggregatorApiMappingProfile()
    : AssemblyScanningMappingProfile(typeof(BffAggregatorApiMappingProfile).Assembly);