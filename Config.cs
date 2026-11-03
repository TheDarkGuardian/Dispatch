using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace Dispatch;

public class DispatchConfig : BasePluginConfig
{
    [JsonPropertyName("PluginAdi")]
    public string PluginAdi { get; set; } = "AFKManager";
}
