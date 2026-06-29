using System.IO.Compression;
using System.Reflection;

namespace DigiTrade.Common.Extensions;

/// <summary>
/// Extensions fot system IO types.
/// </summary>
public static class FileSystemExtensions
{
    /// <summary>
    /// Reads Embedded Resource.
    /// </summary>
    public static string? ReadEmbeddedResource<T>(this string fileName)
    {
        using var stream = ReadEmbeddedResourceStream<T>(fileName);
        if(stream == null) return null;

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static Stream? ReadEmbeddedResourceStream<T>(this string fileName)
    {
        var assembly = typeof(T).GetTypeInfo().Assembly;
        var name = assembly.GetManifestResourceNames()
            .SingleOrDefault(r => r.Contains($".{fileName}", StringComparison.InvariantCultureIgnoreCase));
        if(name == null) return null;

        return assembly.GetManifestResourceStream(name);
    }

    public static string? GetEmbeddedResourceFullName<T>(this string fileName)
    {
        var assembly = typeof(T).GetTypeInfo().Assembly;
        var name = assembly.GetManifestResourceNames()
            .SingleOrDefault(r => r.Contains($".{fileName}", StringComparison.InvariantCultureIgnoreCase));
        return name;
    }

    public static string? GetEmbeddedResourceFullName(this string fileName, Type typeInAssembly)
    {
        var assembly = typeInAssembly.GetTypeInfo().Assembly;
        var name = assembly.GetManifestResourceNames()
            .SingleOrDefault(r => r.Contains($".{fileName}", StringComparison.InvariantCultureIgnoreCase));
        return name;
    }

    /// <summary>
    /// Create a zip file stream from given Directory
    /// </summary>
    public static byte[] Compress(this DirectoryInfo directorySelected)
    {
        using var zipStream = new MemoryStream();
        using var zip = new ZipArchive(zipStream, ZipArchiveMode.Create, true);
        foreach (var fileToCompress in directorySelected.GetFiles())
        {
            using var fileStream = fileToCompress.OpenRead();
            var entry = zip.CreateEntry(Path.GetFileName(fileStream.Name));
            using var entryStream = entry.Open();
            fileStream.CopyTo(entryStream);
        }

        return zipStream.ToArray();
    }

    public static DirectoryInfo AsDirInfo(this string path)
    {
        var dir = new DirectoryInfo(path);
        if(!dir.Exists)
            dir.Create();
        return dir;
    }
}