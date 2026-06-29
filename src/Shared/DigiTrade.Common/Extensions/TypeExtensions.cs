using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

namespace DigiTrade.Common.Extensions;

public static class TypeExtensions
{
    public static string[] GetNestedIncludedFields(this Type classType)
    {
        var list = new List<string>();
        list.AddRange(classType.GetFieldNames());
        foreach(var nestedClass in classType.GetNestedTypes())
        {
            list.AddRange(nestedClass.GetFieldNames());
        }

        return list.OrderBy(static x => x).ToArray();
    }

    public static IEnumerable<string> GetFieldNames(this Type classType)
    {
        var fields = classType.GetFields(BindingFlags.Public | BindingFlags.Static);
        return fields.Select(static field => field.GetValue(null)?.ToString()).Where(static x => x != null).Cast<string>();
    }

    public static string GetFullNameWithAssemblyName(this Type type)
    {
        return type.FullName + ", " + type.Assembly.GetName().Name;
    }

    public static bool IsAssignableTo<TTarget>([MaybeNull] this Type type)
    {
        Check.NotNull(type, nameof(type));

        return type.IsAssignableTo(typeof(TTarget));
    }

    public static bool IsAssignableTo(this Type type, Type targetType)
    {
        Check.NotNull(type, nameof(type));
        Check.NotNull(targetType, nameof(targetType));

        return targetType.IsAssignableFrom(type);
    }

    public static Type[] GetBaseClasses(this Type type, bool includeObject = true)
    {
        Check.NotNull(type, nameof(type));

        var types = new List<Type>();
        if(type.BaseType != null)
        {
            AddTypeAndBaseTypesRecursively(types, type.BaseType, includeObject);
        }
        return types.ToArray();
    }

    public static Type[] GetBaseClasses(this Type type, Type stoppingType, bool includeObject = true)
    {
        Check.NotNull(type, nameof(type));

        var types = new List<Type>();
        if(type.BaseType != null)
        {
            AddTypeAndBaseTypesRecursively(types, type.BaseType, includeObject, stoppingType);
        }
        return types.ToArray();
    }

    public static string? GetTypeDisplayName(this object? item, bool fullName = true)
    {
        return item == null ? null : GetTypeDisplayName(item.GetType(), fullName);
    }

    public static void CopyPropertiesTo(this object source, object destination)
    {
        var propertyValues = source.GetType().GetProperties()
            .ToDictionary(p => p.Name, info => info.GetValue(source));

        foreach (var property in destination.GetType().GetProperties())
        {
            if (propertyValues.TryGetValue(property.Name, out object? value))
            {
                property.SetValue(destination, value);
            }
        }
    }

    public static string GetTypeDisplayName(Type type, bool fullName = true,
        bool includeGenericParameterNames = false,
        bool includeGenericParameters = true, char nestedTypeDelimiter = DefaultNestedTypeDelimiter)
    {
        var builder = new StringBuilder();
        ProcessType(builder, type,
            new DisplayNameOptions(fullName, includeGenericParameterNames, includeGenericParameters,
                nestedTypeDelimiter));
        return builder.ToString();
    }

    public static IEnumerable<Type> ApplyForTypesInAssembly(
        this Type @interface, Action<Type> action, Assembly? source = null)
    {
        var types = (source ?? @interface.Assembly).GetExportedTypes()
            .Where(t =>
                t.GetInterfaces()
                    .Any(i =>
                        (i.IsGenericType && i.GetGenericTypeDefinition() == @interface) ||
                        i.GetTypeInfo() == @interface.GetTypeInfo()
                    ))
            .ToList();
        foreach (var type in types)
        {
            action?.Invoke(type);
        }

        return types;
    }

    public static IEnumerable<Type> ApplyForStructsInAssembly(
        this Type @interface, Action<Type> action, Assembly? source = null)
    {
        var types = (source ?? @interface.Assembly).GetTypes()
            .Where(t => t.IsValueType &&
                        t.GetInterfaces()
                            .Any(i =>
                                (i.IsGenericType && i.GetGenericTypeDefinition() == @interface) ||
                                i.GetTypeInfo() == @interface.GetTypeInfo()
                            ))
            .ToList();
        foreach (var type in types)
        {
            action?.Invoke(type);
        }

        return types;
    }

    #region Private
    private static void AddTypeAndBaseTypesRecursively(
        List<Type> types,
        Type? type,
        bool includeObject,
        Type? stoppingType = null)
    {
        if(type == null || type == stoppingType)
        {
            return;
        }

        if(!includeObject && type == typeof(object))
        {
            return;
        }

        if(type.BaseType != null)
        {
            AddTypeAndBaseTypesRecursively(types, type.BaseType, includeObject, stoppingType);
        }
        types.Add(type);
    }

    private const char DefaultNestedTypeDelimiter = '+';

    private static readonly Dictionary<Type, string> BuiltInTypeNames = new()
    {
        {typeof(void), "void"},
        {typeof(bool), "bool"},
        {typeof(byte), "byte"},
        {typeof(char), "char"},
        {typeof(decimal), "decimal"},
        {typeof(double), "double"},
        {typeof(float), "float"},
        {typeof(int), "int"},
        {typeof(long), "long"},
        {typeof(object), "object"},
        {typeof(sbyte), "sbyte"},
        {typeof(short), "short"},
        {typeof(string), "string"},
        {typeof(uint), "uint"},
        {typeof(ulong), "ulong"},
        {typeof(ushort), "ushort"}
    };

    private static void ProcessType(StringBuilder builder, Type type, in DisplayNameOptions options)
    {
        if(type.IsGenericType)
        {
            Type[] genericArguments = type.GetGenericArguments();
            ProcessGenericType(builder, type, genericArguments, genericArguments.Length, options);
        }
        else if(type.IsArray)
        {
            ProcessArrayType(builder, type, options);
        }
        else if(BuiltInTypeNames.TryGetValue(type, out string? builtInName))
        {
            builder.Append(builtInName);
        }
        else if(type.IsGenericParameter)
        {
            if(options.IncludeGenericParameterNames)
            {
                builder.Append(type.Name);
            }
        }
        else
        {
            string name = options.FullName ? type.FullName! : type.Name;
            builder.Append(name);

            if(options.NestedTypeDelimiter != DefaultNestedTypeDelimiter)
            {
                builder.Replace(DefaultNestedTypeDelimiter, options.NestedTypeDelimiter, builder.Length - name.Length,
                    name.Length);
            }
        }
    }

    private static void ProcessArrayType(StringBuilder builder, Type type, in DisplayNameOptions options)
    {
        Type innerType = type;
        while(innerType.IsArray)
        {
            innerType = innerType.GetElementType()!;
        }

        ProcessType(builder, innerType, options);

        while(type.IsArray)
        {
            builder.Append('[');
            builder.Append(',', type.GetArrayRank() - 1);
            builder.Append(']');
            type = type.GetElementType()!;
        }
    }

    private static void ProcessGenericType(StringBuilder builder, Type type, Type[] genericArguments, int length,
        in DisplayNameOptions options)
    {
        int offset = 0;
        if(type.IsNested)
        {
            offset = type.DeclaringType!.GetGenericArguments().Length;
        }

        if(options.FullName)
        {
            if(type.IsNested)
            {
                ProcessGenericType(builder, type.DeclaringType!, genericArguments, offset, options);
                builder.Append(options.NestedTypeDelimiter);
            }
            else if(!string.IsNullOrEmpty(type.Namespace))
            {
                builder.Append(type.Namespace);
                builder.Append('.');
            }
        }

        int genericPartIndex = type.Name.IndexOf('`');
        if(genericPartIndex <= 0)
        {
            builder.Append(type.Name);
            return;
        }

        builder.Append(type.Name, 0, genericPartIndex);

        if(options.IncludeGenericParameters)
        {
            builder.Append('<');
            for(int i = offset; i < length; i++)
            {
                ProcessType(builder, genericArguments[i], options);
                if(i + 1 == length)
                {
                    continue;
                }

                builder.Append(',');
                if(options.IncludeGenericParameterNames || !genericArguments[i + 1].IsGenericParameter)
                {
                    builder.Append(' ');
                }
            }

            builder.Append('>');
        }
    }

    private readonly struct DisplayNameOptions(
        bool fullName,
        bool includeGenericParameterNames,
        bool includeGenericParameters,
        char nestedTypeDelimiter)
    {
        public bool FullName { get; } = fullName;

        public bool IncludeGenericParameters { get; } = includeGenericParameters;

        public bool IncludeGenericParameterNames { get; } = includeGenericParameterNames;

        public char NestedTypeDelimiter { get; } = nestedTypeDelimiter;
    }

    #endregion
}