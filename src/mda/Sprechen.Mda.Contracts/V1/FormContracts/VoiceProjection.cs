using System.Text.Json.Serialization;
using Sprechen.Mda.Contracts.V1.FormVoice;

namespace Sprechen.Mda.Contracts.V1.FormContracts;

/// <summary>
/// Aufgelöste Sprachprojektion auf Vertragsebene.
/// </summary>
public sealed class VoiceProjection
{
    /// <summary>
    /// Optionale Sprachhinweise mit Ansagen, Intents und Synonymtabellen.
    /// </summary>
    [JsonPropertyName("hints")]
    public VoiceHintsDefinition? Hints { get; set; }

    /// <summary>
    /// Optionales aufgelöstes Laufzeitprofil für die Spracheinstellungen des Formulardialogs.
    /// </summary>
    [JsonPropertyName("formVoiceProfile")]
    public FormVoiceProfile? FormVoiceProfile { get; set; }
}
