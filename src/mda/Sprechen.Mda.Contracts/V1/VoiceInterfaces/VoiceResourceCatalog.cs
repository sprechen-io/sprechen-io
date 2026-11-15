using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.VoiceInterfaces;

/// <summary>
/// Katalog wiederverwendbarer sprachabhängiger Textressourcen für die Sprachoberfläche.
/// </summary>
public sealed class VoiceResourceCatalog
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
    [JsonPropertyName("resourceModelId")]
    public string ResourceModelId { get; set; } = string.Empty;

    /// <summary>
    /// Sprachabhängige Textressourcen für Sitzungseinstieg und allgemeine Oberfläche.
    /// </summary>
    [JsonPropertyName("resources")]
    public List<VoiceInteractionResource> Resources { get; set; } = [];
}


