using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.VoiceInterfaces;

/// <summary>
/// Sprachprofil für Ja-/Nein-Erkennung und Standardwörter.
/// </summary>
public sealed class VoiceLexiconYesNoProfile
{
    /// <summary>
    /// ISO-639-1-Sprachcode des Profils.
    /// </summary>
    [JsonPropertyName("languageCode")]
    public string LanguageCode { get; set; } = string.Empty;

    /// <summary>
    /// Bevorzugtes Standardwort für Zustimmung.
    /// </summary>
    [JsonPropertyName("yesWord")]
    public string YesWord { get; set; } = string.Empty;

    /// <summary>
    /// Bevorzugtes Standardwort für Ablehnung.
    /// </summary>
    [JsonPropertyName("noWord")]
    public string NoWord { get; set; } = string.Empty;

    /// <summary>
    /// Erkannte Formulierungen für Zustimmung.
    /// </summary>
    [JsonPropertyName("yesTokens")]
    public List<string> YesTokens { get; set; } = [];

    /// <summary>
    /// Erkannte Formulierungen für Ablehnung.
    /// </summary>
    [JsonPropertyName("noTokens")]
    public List<string> NoTokens { get; set; } = [];

    /// <summary>
    /// Verneinungspartikeln, die ein gleichzeitig genanntes Ja-Wort entkräften.
    /// Direkte Nein-Wörter stehen getrennt in <see cref="NoTokens"/>.
    /// </summary>
    [JsonPropertyName("negationCues")]
    public List<string> NegationCues { get; set; } = [];

    /// <summary>
    /// Formulierungen, mit denen eine Person Nichtverstehen ausdrückt.
    /// Sie werden vor Ja-/Nein-Treffern berücksichtigt, damit etwa „Non capisco“
    /// nicht wegen eines enthaltenen Nein-Worts als Ablehnung gilt.
    /// </summary>
    [JsonPropertyName("notUnderstoodPhrases")]
    public List<string> NotUnderstoodPhrases { get; set; } = [];

}


