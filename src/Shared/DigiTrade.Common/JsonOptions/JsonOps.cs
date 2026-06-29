using System.Text.Json;

namespace DigiTrade.Common.JsonOptions;

public class JsonOps
{
    public JsonSerializerOptions Serialize { get; }
    public JsonSerializerOptions Deserialize { get; }
    public JsonOps()
    {
        Serialize = JsonSettingProvider.JsonSerializerOps;
        // Only place we need GetSerializerOptions().
        Deserialize = JsonSettingProvider.GetSerializerOptions();
        Deserialize.WriteIndented = true;
    }
}