using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.VoiceInterfaces;

/// <summary>
/// Verhalten bei Mikrofonausfällen während des Wartens auf ein Aktivierungswort.
/// Die Regel gilt für die gesamte Sitzung, auch nach erfolgreichen Aktivierungen.
/// </summary>
public sealed class MicrophoneAvailabilityPolicy
{
    /// <summary>
    /// Anzahl aufeinanderfolgender Ausfälle vor dem ersten gesprochenen Hinweis.
    /// </summary>
    [JsonPropertyName("failureThreshold")]
    public int FailureThreshold { get; set; }

    /// <summary>
    /// Mindestabstand zwischen gesprochenen Ausfallhinweisen in Sekunden.
    /// </summary>
    [JsonPropertyName("cooldownSeconds")]
    public int CooldownSeconds { get; set; }

    /// <summary>
    /// Höchstdauer der exponentiell wachsenden Pause zwischen Ausfallhinweisen in Sekunden.
    /// </summary>
    [JsonPropertyName("maxCooldownSeconds")]
    public int MaxCooldownSeconds { get; set; }

    /// <summary>
    /// Ob Aufnahmeversuche ohne neues Sprachkommando fortgesetzt werden.
    /// </summary>
    [JsonPropertyName("automaticRetry")]
    public bool AutomaticRetry { get; set; }

    /// <summary>
    /// Ob die wieder verfügbare Aufnahme nach einem angekündigten Ausfall angesagt wird.
    /// </summary>
    [JsonPropertyName("announceRecovery")]
    public bool AnnounceRecovery { get; set; }
}
