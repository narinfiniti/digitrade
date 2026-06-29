using System.Runtime.CompilerServices;
using System.Reflection;
using AutoMapper;

namespace DigiTrade.Common.Mapping;

public abstract class AssemblyScanningMappingProfile : Profile
{
    protected AssemblyScanningMappingProfile(params Assembly[] assemblies)
    {
        AllowNullCollections = true;
        ApplyCommonMappings();
        ApplyMappingsFromAssemblies(assemblies);
    }

    protected virtual void ApplyCommonMappings()
    {
        CreateMap<short?, short>().ConvertUsing((src, dest) => src ?? dest);
        CreateMap<byte?, byte>().ConvertUsing((src, dest) => src ?? dest);
        CreateMap<int?, int>().ConvertUsing((src, dest) => src ?? dest);
        CreateMap<bool?, bool>().ConvertUsing((src, dest) => src ?? dest);
        CreateMap<decimal?, decimal>().ConvertUsing((src, dest) => src ?? dest);
        CreateMap<Guid?, Guid>().ConvertUsing((src, dest) => src ?? dest);
        CreateMap<DateTime?, DateTime>().ConvertUsing((src, dest) => src ?? dest);
    }

    private void ApplyMappingsFromAssemblies(params Assembly[] assemblies)
    {
        var mapTypes = assemblies.SelectMany(x => x.GetExportedTypes())
            .Where(t =>
                t.GetInterfaces()
                    .Any(i =>
                        i.IsGenericType &&
                        i.GetGenericTypeDefinition() == typeof(IAutoMap<,>)));
        const string methodName = "CreateMap";
        foreach (var type in mapTypes)
        {
            var instance = CreateMapInstance(type);
            var methodInfo = type.GetMethod(methodName)
                ?? type.GetInterface("IAutoMap`2")
                    ?.GetMethod(methodName);
            methodInfo?.Invoke(instance, new object[] { this });
        }
    }

    private static object CreateMapInstance(Type type)
    {
        try
        {
            return Activator.CreateInstance(type) ?? RuntimeHelpers.GetUninitializedObject(type);
        }
        catch (MissingMethodException)
        {
            return RuntimeHelpers.GetUninitializedObject(type);
        }
    }
}