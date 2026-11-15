using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.VoiceInterfaces;

/// <summary>
/// Katalog akustischer Aktivierungswortdetektoren für die Sprachoberfläche.
/// </summary>
public sealed class WakeWordDetectorCatalog
{
    /// <summary>
    /// Version des Katalogformats.
    /// </summary>
    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; set; } = string.Empty;

    /// <summary>
    /// Inhaltsversion des Katalogs ohne vorgegebene Versionsstruktur.
    /// </summary>
    [JsonPropertyName("catalogVersion")]
    public string CatalogVersion { get; set; } = string.Empty;

    /// <summary>
    /// Stabile Modellkennung für Katalogreferenzen.
    /// </summary>
    [JsonPropertyName("detectorCatalogModelId")]
    public string DetectorCatalogModelId { get; set; } = string.Empty;

    /// <summary>
    /// Kennung des Aktivierungswortkatalogs, für den diese Detektorprofile gelten.
    /// </summary>
    [JsonPropertyName("wakeWordModelId")]
    public string WakeWordModelId { get; set; } = string.Empty;

    /// <summary>
    /// Akustische Detektorprofile für den referenzierten Aktivierungswortkatalog.
    /// </summary>
    [JsonPropertyName("detectorProfiles")]
    public List<WakeWordDetectorProfile> DetectorProfiles { get; set; } = [];
}
