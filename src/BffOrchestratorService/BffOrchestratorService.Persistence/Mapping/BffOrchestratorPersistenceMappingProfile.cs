using AutoMapper;
using ProcessQueueItemEntity = BffOrchestratorService.Domain.Entities.ProcessQueueItem;
using ProcessQueueItemModel = BffOrchestratorService.Domain.Models.ProcessQueueItemModel;

namespace BffOrchestratorService.Persistence.Mapping;

public sealed class BffOrchestratorPersistenceMappingProfile : Profile
{
    public BffOrchestratorPersistenceMappingProfile()
    {
        CreateMap<ProcessQueueItemModel, ProcessQueueItemEntity>().ReverseMap();
    }
}