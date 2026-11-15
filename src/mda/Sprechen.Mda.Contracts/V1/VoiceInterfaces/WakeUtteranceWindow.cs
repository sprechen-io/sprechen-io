using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.VoiceInterfaces;

/// <summary>
/// Grenzen für das Zusammenführen aufeinanderfolgender Transkripte
/// zu einer vollständigen Äußerung mit Aktivierungswort.
/// </summary>
public sealed class WakeUtteranceWindow
{
    /// <summary>
    /// Maximale Pause zwischen zwei Aufnahmezyklen in Millisekunden,
    /// bei der deren Transkripte noch zusammengeführt werden dürfen.
    /// </summary>
    [JsonPropertyName("maxInterCyclePauseMs")]
    public int MaxInterCyclePauseMs { get; set; }

    /// <summary>
    /// Höchstens übernommene Anzahl vorheriger Aufnahmezyklen.
    /// </summary>
    [JsonPropertyName("maxCarriedCycles")]
    public int MaxCarriedCycles { get; set; }

    /// <summary>
    /// Ob das erste übernommene Transkript ein echtes Präfix einer Aktivierungsform sein muss.
    /// </summary>
    [JsonPropertyName("prefixOnlyFirstCycleAccepted")]
    public bool PrefixOnlyFirstCycleAccepted { get; set; }
}
