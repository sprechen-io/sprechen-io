using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.VoiceInterfaces;

/// <summary>
/// Profil eines akustischen Detektors für einen Aktivierungswortbestand.
/// </summary>
public sealed class WakeWordDetectorProfile
{
    /// <summary>
    /// Stabile Kennung des Detektorprofils.
    /// </summary>
    [JsonPropertyName("detectorId")]
    public string DetectorId { get; set; } = string.Empty;

    /// <summary>
    /// Betriebsmodus des Profils.
    /// </summary>
    [JsonPropertyName("operatingMode")]
    public WakeWordDetectorOperatingMode OperatingMode { get; set; }

    /// <summary>
    /// Schwellenwert der Erkennungssicherheit für akustische Treffer.
    /// </summary>
    [JsonPropertyName("confidenceThreshold")]
    public double ConfidenceThreshold { get; set; }

    /// <summary>
    /// Sperrzeit nach erfolgreicher Aktivierung in Millisekunden.
    /// </summary>
    [JsonPropertyName("refractoryMs")]
    public int RefractoryMs { get; set; }

    /// <summary>
    /// Vorbereitungszeit des Detektors in Millisekunden.
    /// </summary>
    [JsonPropertyName("warmupMs")]
    public int WarmupMs { get; set; }

    /// <summary>
    /// Zeitspanne vor der aktuellen Verarbeitungsposition für die Auswertung akustischer
    /// Signale, in Millisekunden. Sie steuert keine anschließende Aufnahme.
    /// </summary>
    [JsonPropertyName("lookbackMs")]
    public int LookbackMs { get; set; }

    /// <summary>
    /// Zulässigkeit des Detektors bei benutzerseitig geänderten Aktivierungswörtern.
    /// </summary>
    [JsonPropertyName("eligibilityPolicy")]
    public WakeWordDetectorEligibilityPolicy EligibilityPolicy { get; set; }

    /// <summary>
    /// Begründungscodes für die ersatzweise Erkennung im Transkript.
    /// </summary>
    [JsonPropertyName("fallbackReasonCodes")]
    public WakeWordDetectorFallbackReasonCodes FallbackReasonCodes { get; set; } = new();
}
