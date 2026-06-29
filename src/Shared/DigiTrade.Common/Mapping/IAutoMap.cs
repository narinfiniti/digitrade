using AutoMapper;

namespace DigiTrade.Common.Mapping;

public interface IAutoMap<TSource, TDestination>
{
    void CreateMap(Profile profile)
    {
        profile.CreateMap<TSource, TDestination>();
        profile.CreateMap<TDestination, TSource>()
            .ForAllMembers(opt => opt.Condition((src, dest, mbr) => mbr != null));
    }
}