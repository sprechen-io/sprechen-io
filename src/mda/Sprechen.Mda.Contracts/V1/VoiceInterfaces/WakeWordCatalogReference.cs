using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.VoiceInterfaces;

/// <summary>
/// Referenz auf einen getrennten Aktivierungswortkatalog.
/// </summary>
public sealed class WakeWordCatalogReference
{
    /// <summary>
    /// Kennung des referenzierten Aktivierungswortkatalogs.
    /// </summary>
    [JsonPropertyName("wakeWordModelId")]
    public string WakeWordModelId { get; set; } = string.Empty;

    /// <summary>
    /// Version des referenzierten Aktivierungswortkatalogs.
    /// </summary>
    [JsonPropertyName("catalogVersion")]
    public string CatalogVersion { get; set; } = string.Empty;
}


