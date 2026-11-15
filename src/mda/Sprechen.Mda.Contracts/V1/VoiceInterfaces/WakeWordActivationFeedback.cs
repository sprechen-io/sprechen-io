using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.VoiceInterfaces;

/// <summary>
/// Rückmeldung nach erfolgreicher Aktivierungsworterkennung.
/// Ein Hinweiston macht die Aktivierung hörbar, bevor die nächste Ansage beginnt.
/// </summary>
public sealed class WakeWordActivationFeedback
{
    /// <summary>
    /// Kennung des Hinweistons für eine erfolgreiche Aktivierung.
    /// </summary>
    [JsonPropertyName("activatedEarconId")]
    public string ActivatedEarconId { get; set; } = string.Empty;
}
