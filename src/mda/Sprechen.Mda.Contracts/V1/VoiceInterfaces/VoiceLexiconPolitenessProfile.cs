using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.VoiceInterfaces;

/// <summary>
/// Sprachabhängige Formulierungen zur Wahl einer formellen oder informellen Anrede
/// beim Sitzungseinstieg und im Sprachdialog.
/// </summary>
public sealed class VoiceLexiconPolitenessProfile
{
    /// <summary>
    /// ISO-639-1-Sprachcode des Profils.
    /// </summary>
    [JsonPropertyName("languageCode")]
    public string LanguageCode { get; set; } = string.Empty;

    /// <summary>
    /// Bevorzugte Bezeichnung für die formelle Anrede.
    /// </summary>
    [JsonPropertyName("formalWord")]
    public string FormalWord { get; set; } = string.Empty;

    /// <summary>
    /// Bevorzugte Bezeichnung für die informelle Anrede.
    /// </summary>
    [JsonPropertyName("informalWord")]
    public string InformalWord { get; set; } = string.Empty;

    /// <summary>
    /// Erkannte Formulierungen für die formelle Anrede.
    /// </summary>
    [JsonPropertyName("formalTokens")]
    public List<string> FormalTokens { get; set; } = [];

    /// <summary>
    /// Erkannte Formulierungen für die informelle Anrede.
    /// </summary>
    [JsonPropertyName("informalTokens")]
    public List<string> InformalTokens { get; set; } = [];
}


