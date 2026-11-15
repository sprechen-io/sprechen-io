using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.VoiceInterfaces;

/// <summary>
/// Dokumentmodell der Sprachoberfläche mit Referenzen auf getrennte Text-,
/// Ansagen-, Lexikon- und Aktivierungswortkataloge.
/// </summary>
public sealed class VoiceInterfaceDocument
{
    /// <summary>
    /// Schemahauptversion des Sprachoberflächendokuments.
    /// </summary>
    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; set; } = string.Empty;

    /// <summary>
    /// Inhaltsversion des Dokuments.
    /// </summary>
    [JsonPropertyName("catalogVersion")]
    public string CatalogVersion { get; set; } = string.Empty;

    /// <summary>
    /// Stabile Modellkennung der Sprachoberfläche.
    /// </summary>
    [JsonPropertyName("voiceInterfaceId")]
    public string VoiceInterfaceId { get; set; } = string.Empty;

    /// <summary>
    /// Unterstützte Basissprachen.
    /// </summary>
    [JsonPropertyName("supportedLanguages")]
    public List<string> SupportedLanguages { get; set; } = [];

    /// <summary>
    /// Standardsprache für die Textauflösung bei fehlender Übersetzung.
    /// </summary>
    [JsonPropertyName("defaultLanguage")]
    public string DefaultLanguage { get; set; } = string.Empty;

    /// <summary>
    /// Ob alle unterstützten Sprachen vollständig übersetzt sein müssen.
    /// </summary>
    [JsonPropertyName("strictLocalization")]
    public bool StrictLocalization { get; set; }

    /// <summary>
    /// Ob beim Sitzungseinstieg ein Sprachwechsel angeboten wird. Ohne diese Option
    /// entfällt die zusätzliche Sprachfrage. So kann die Studie denselben Einstieg vorgeben.
    /// </summary>
    [JsonPropertyName("sessionLanguageOfferEnabled")]
    public bool SessionLanguageOfferEnabled { get; set; }

    /// <summary>
    /// Maximale Wartezeit bis zum ersten erfolgreich eingereihten PCM-Block einer Sprachausgabe.
    /// </summary>
    [JsonPropertyName("firstPcmEnqueueTimeoutMs")]
    public int FirstPcmEnqueueTimeoutMs { get; set; }

    /// <summary>
    /// Lokaler Hinweiston bei ausbleibendem ersten PCM-Block, unabhängig vom TTS-Anbieter.
    /// </summary>
    [JsonPropertyName("firstPcmTimeoutEarconId")]
    public string FirstPcmTimeoutEarconId { get; set; } = string.Empty;


    /// <summary>
    /// Referenz auf den Katalog sprachabhängiger Textressourcen.
    /// </summary>
    [JsonPropertyName("resourceCatalog")]
    public VoiceResourceCatalogReference ResourceCatalog { get; set; } = new();

    /// <summary>
    /// Referenz auf den allgemeinen Ansagenkatalog.
    /// </summary>
    [JsonPropertyName("promptCatalog")]
    public GenericPromptCatalogReference PromptCatalog { get; set; } = new();

    /// <summary>
    /// Referenz auf den Sprachlexikonkatalog.
    /// </summary>
    [JsonPropertyName("lexiconCatalog")]
    public VoiceLexiconCatalogReference LexiconCatalog { get; set; } = new();

    /// <summary>
    /// Referenz auf den Aktivierungswortkatalog.
    /// </summary>
    [JsonPropertyName("wakeWordCatalog")]
    public WakeWordCatalogReference WakeWordCatalog { get; set; } = new();

    /// <summary>
    /// Referenz auf den Katalog akustischer Aktivierungswortdetektoren.
    /// </summary>
    [JsonPropertyName("wakeWordDetectorCatalog")]
    public WakeWordDetectorCatalogReference WakeWordDetectorCatalog { get; set; } = new();
}


