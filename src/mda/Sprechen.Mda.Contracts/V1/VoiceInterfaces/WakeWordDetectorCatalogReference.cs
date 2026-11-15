using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.VoiceInterfaces;

/// <summary>
/// Referenz auf einen getrennten Katalog akustischer Aktivierungswortdetektoren.
/// </summary>
public sealed class WakeWordDetectorCatalogReference
{
    /// <summary>
    /// Kennung des referenzierten Detektorkatalogs.
    /// </summary>
    [JsonPropertyName("detectorCatalogModelId")]
    public string DetectorCatalogModelId { get; set; } = string.Empty;

    /// <summary>
    /// Version des referenzierten Detektorkatalogs.
    /// </summary>
    [JsonPropertyName("catalogVersion")]
    public string CatalogVersion { get; set; } = string.Empty;
}
