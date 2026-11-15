using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.VoiceInterfaces;

/// <summary>
/// Begründungscodes für den Wechsel von akustischer Aktivierungsworterkennung
/// zur Erkennung im Transkript.
/// </summary>
public sealed class WakeWordDetectorFallbackReasonCodes
{
    /// <summary>
    /// Das Profil schreibt ausschließlich die Erkennung im Transkript vor.
    /// </summary>
    [JsonPropertyName("catalogFallbackOnly")]
    public string CatalogFallbackOnly { get; set; } = string.Empty;

    /// <summary>
    /// Eine benutzerseitige Änderung des Aktivierungsworts schließt den akustischen Detektor aus.
    /// </summary>
    [JsonPropertyName("overrideIneligible")]
    public string OverrideIneligible { get; set; } = string.Empty;

    /// <summary>
    /// Aktivierungsform, die nur zusätzlich erkannt und nicht angeboten wird.
    /// </summary>
    [JsonPropertyName("recognitionOnlyActivation")]
    public string RecognitionOnlyActivation { get; set; } = string.Empty;

    /// <summary>
    /// Kein funktionsfähiger akustischer Detektor verfügbar.
    /// </summary>
    [JsonPropertyName("detectorUnavailable")]
    public string DetectorUnavailable { get; set; } = string.Empty;

    /// <summary>
    /// Akustischer Detektor noch in der Vorbereitungsphase.
    /// </summary>
    [JsonPropertyName("detectorWarmup")]
    public string DetectorWarmup { get; set; } = string.Empty;

    /// <summary>
    /// Akustischer Detektor nach dem Start ausgefallen.
    /// </summary>
    [JsonPropertyName("detectorFailure")]
    public string DetectorFailure { get; set; } = string.Empty;
}
