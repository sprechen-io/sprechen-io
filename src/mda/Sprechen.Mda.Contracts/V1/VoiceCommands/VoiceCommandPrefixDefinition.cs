using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.VoiceCommands;

/// <summary>
/// Gesprochenes Kommandopräfix. Die Kategorie für Eingabepräfixe ist reserviert.
/// </summary>
public sealed class VoiceCommandPrefixDefinition
{
    /// <summary>
    /// Präfixart gemäß VoiceCommandPrefixKinds.
    /// </summary>
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    /// <summary>
    /// Sprachabhängige Präfixformulierungen, beispielsweise Befehl oder Kommando.
    /// </summary>
    [JsonPropertyName("utterances")]
    public List<LocalizedText> Utterances { get; set; } = [];
}
