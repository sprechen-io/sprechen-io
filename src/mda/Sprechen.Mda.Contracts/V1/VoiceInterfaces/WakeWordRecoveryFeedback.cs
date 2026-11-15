using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.VoiceInterfaces;

/// <summary>
/// Akustische Rückmeldung, wenn während des Wartens auf ein Aktivierungswort
/// eine Äußerung ohne passende Aktivierungsform erkannt wird.
/// </summary>
public sealed class WakeWordRecoveryFeedback
{
    /// <summary>
    /// Kennung des Hinweistons für ein nicht erkanntes Aktivierungswort.
    /// </summary>
    [JsonPropertyName("notRecognizedEarconId")]
    public string NotRecognizedEarconId { get; set; } = string.Empty;

    /// <summary>
    /// Anzahl aufeinanderfolgender Fehlversuche vor der ersten erfolgreichen Aktivierung,
    /// ab der eine gesprochene Hilfe den Hinweiston ersetzt. Fehlen alle Hilfeeinstellungen,
    /// wird ausschließlich der Hinweiston verwendet.
    /// </summary>
    [JsonPropertyName("guidanceMissThreshold")]
    public int? GuidanceMissThreshold { get; set; }

    /// <summary>
    /// Mindestabstand zwischen gesprochenen Hilfen in Sekunden. Der Abstand begrenzt
    /// die Häufigkeit von Hinweisen bei wiederholt erkannter Umgebungssprache.
    /// </summary>
    [JsonPropertyName("guidanceCooldownSeconds")]
    public int? GuidanceCooldownSeconds { get; set; }

    /// <summary>
    /// Höchstabstand zwischen gesprochenen Hilfen in Sekunden, mindestens
    /// <c>guidanceCooldownSeconds</c>. Nach jeder Hilfe verdoppelt sich der Abstand
    /// bis zu diesem Höchstwert. Die Hilfe bleibt ohne feste Anzahlgrenze verfügbar
    /// und endet nach der ersten erfolgreichen Aktivierung.
    /// </summary>
    [JsonPropertyName("guidanceMaxCooldownSeconds")]
    public int? GuidanceMaxCooldownSeconds { get; set; }
}
