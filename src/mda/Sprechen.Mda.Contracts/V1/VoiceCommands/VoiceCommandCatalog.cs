using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.VoiceCommands;

/// <summary>
/// Eigenständiges Modell globaler Sprachkommandos.
/// </summary>
public sealed class VoiceCommandCatalog
{
    /// <summary>
    /// Version des JSON-Schemas zur Validierung des Kommandomodells.
    /// </summary>
    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; set; } = string.Empty;

    /// <summary>
    /// Inhaltsversion des Kommandokatalogs.
    /// </summary>
    [JsonPropertyName("catalogVersion")]
    public string CatalogVersion { get; set; } = string.Empty;

    /// <summary>
    /// Stabile Modellkennung zur Auswahl eines Kommandokatalogs.
    /// </summary>
    [JsonPropertyName("commandModelId")]
    public string CommandModelId { get; set; } = string.Empty;

    /// <summary>
    /// Unterstützte Basissprachen als kleingeschriebene ISO-639-1-Kennungen.
    /// </summary>
    [JsonPropertyName("supportedLanguages")]
    public List<string> SupportedLanguages { get; set; } = [];

    /// <summary>
    /// Optionale Standardsprache bei fehlender Übersetzung.
    /// </summary>
    [JsonPropertyName("defaultLanguage")]
    public string? DefaultLanguage { get; set; }

    /// <summary>
    /// Bei true gelten fehlende Übersetzungen als Fehler. Es wird nicht still auf eine andere Sprache zurückgegriffen.
    /// </summary>
    [JsonPropertyName("strictLocalization")]
    public bool StrictLocalization { get; set; }

    /// <summary>
    /// Globale Sprachkommandos mit sprachabhängigen Formulierungen.
    /// </summary>
    [JsonPropertyName("commands")]
    public List<VoiceCommandDefinition> Commands { get; set; } = [];

    /// <summary>
    /// Präfixdefinitionen für Kommandos. Die Kategorie für Eingabepräfixe ist reserviert.
    /// </summary>
    [JsonPropertyName("prefixes")]
    public List<VoiceCommandPrefixDefinition> Prefixes { get; set; } = [];
}
