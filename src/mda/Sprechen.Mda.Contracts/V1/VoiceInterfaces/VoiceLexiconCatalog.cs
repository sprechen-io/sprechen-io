using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.VoiceInterfaces;

/// <summary>
/// Katalog sprachabhängiger Formulierungen für Ja, Nein und die Wahl der Anrede.
/// </summary>
public sealed class VoiceLexiconCatalog
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
    [JsonPropertyName("lexiconModelId")]
    public string LexiconModelId { get; set; } = string.Empty;

    /// <summary>
    /// Sprachprofile für Ja-/Nein-Erkennung und Standardwörter.
    /// </summary>
    [JsonPropertyName("yesNoProfiles")]
    public List<VoiceLexiconYesNoProfile> YesNoProfiles { get; set; } = [];

    /// <summary>
    /// Sprachprofile für formelle und informelle Anrede.
    /// </summary>
    [JsonPropertyName("politenessProfiles")]
    public List<VoiceLexiconPolitenessProfile> PolitenessProfiles { get; set; } = [];
}


