using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.VoiceInterfaces;

/// <summary>
/// Aufgelöstes Laufzeitmodell der Sprachoberfläche mit Textressourcen, Lexika
/// und Katalogen zur Aktivierungsworterkennung.
/// </summary>
public sealed class VoiceInterface
{
    /// <summary>
    /// Schemahauptversion des aufgelösten Sprachoberflächenmodells.
    /// </summary>
    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; set; } = string.Empty;

    /// <summary>
    /// Inhaltsversion des aufgelösten Katalogs.
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
    /// Sprachabhängige Textressourcen für Sitzungseinstieg und allgemeine Oberfläche.
    /// </summary>
    [JsonPropertyName("resources")]
    public List<VoiceInteractionResource> Resources { get; set; } = [];

    /// <summary>
    /// Aufgelöster Ansagenkatalog mit Ersatztexten für Formulardialoge und Datenschutzregeln.
    /// </summary>
    [JsonPropertyName("promptCatalog")]
    public GenericPromptCatalog PromptCatalog { get; set; } = new();

    /// <summary>
    /// Sprachprofile für Ja-/Nein-Erkennung und Standardwörter.
    /// </summary>
    [JsonPropertyName("yesNoProfiles")]
    public List<VoiceLexiconYesNoProfile> YesNoProfiles { get; set; } = [];

    /// <summary>
    /// Sprachprofile für formelle und informelle Ansprache.
    /// </summary>
    [JsonPropertyName("politenessProfiles")]
    public List<VoiceLexiconPolitenessProfile> PolitenessProfiles { get; set; } = [];

    /// <summary>
    /// Aufgelöster Katalog der Aktivierungswörter.
    /// </summary>
    [JsonPropertyName("wakeWordCatalog")]
    public WakeWordCatalog WakeWordCatalog { get; set; } = new();

    /// <summary>
    /// Aufgelöster Katalog akustischer Aktivierungswortdetektoren.
    /// </summary>
    [JsonPropertyName("wakeWordDetectorCatalog")]
    public WakeWordDetectorCatalog WakeWordDetectorCatalog { get; set; } = new();
}


