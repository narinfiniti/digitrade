using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace DigiTrade.Common.JsonOptions;

/// <summary>
/// Provides custom json serialization settings.
/// </summary>
public static class JsonSettingProvider
{
    public static readonly JsonSerializerOptions JsonSerializerOps = GetSerializerOptions();

    public static JsonSerializerOptions GetSerializerOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            //DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            IncludeFields = true,
            WriteIndented = false,
            ReadCommentHandling = JsonCommentHandling.Disallow,
            AllowTrailingCommas = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never,
            IgnoreReadOnlyProperties = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            MaxDepth = 64,
            DictionaryKeyPolicy = null,
            DefaultBufferSize = 16 * 1024,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };
        //options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    public static readonly JsonNodeOptions JsonNodeOps = new()
    {
        PropertyNameCaseInsensitive = true
    };
}