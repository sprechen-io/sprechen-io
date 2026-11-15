using System.Text.Json.Serialization;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.Mda.Contracts.V1.VoiceCommands;

/// <summary>
/// Globales Sprachkommando mit sprachabhängigen Formulierungen.
/// </summary>
public sealed class VoiceCommandDefinition
{
    /// <summary>
    /// Stabile Kommandokennung, beispielsweise next, help oder switchForm.
    /// </summary>
    [JsonPropertyName("commandId")]
    public CommandId CommandId { get; set; }

    /// <summary>
    /// Sprachabhängige Formulierungen zum Auslösen des Kommandos.
    /// </summary>
    [JsonPropertyName("utterances")]
    public List<LocalizedText> Utterances { get; set; } = [];

    /// <summary>
    /// Optionale Sprechform für die vorgelesene Kommandoliste. Ohne Text in der
    /// angefragten Sprache erfolgt keine Ansage. Nur für die Ausgabe: Sie wird weder
    /// mit Transkripten verglichen noch an den Erkenner geschickt. Es gibt keinen
    /// Ersatz anhand der Position einer Erkennungsformulierung, damit deren Umordnung
    /// den vorgelesenen Text nicht verändert.
    /// </summary>
    [JsonPropertyName("spokenForm")]
    public LocalizedText? SpokenForm { get; set; }

    /// <summary>
    /// Ob das Kommando aktiv ist. Ohne Angabe gilt es als aktiv.
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool? Enabled { get; set; }
}
