using System.Text.Json.Serialization;

namespace Sprechen.Mda.Contracts.V1.VoiceInterfaces;

/// <summary>
/// Katalog der Aktivierungsformen für die Sprachoberfläche. Der Name wird pro Sprache
/// modelliert. Der aktuelle Referenzvertrag verwendet sprachübergreifend „alex“.
/// </summary>
public sealed class WakeWordCatalog
{
    /// <summary>
    /// Version des Katalogformats.
    /// </summary>
    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; set; } = string.Empty;

    /// <summary>
    /// Inhaltsversion des Katalogs ohne vorgegebene Versionsstruktur.
    /// </summary>
    [JsonPropertyName("catalogVersion")]
    public string CatalogVersion { get; set; } = string.Empty;

    /// <summary>
    /// Stabile Modellkennung für Katalogreferenzen.
    /// </summary>
    [JsonPropertyName("wakeWordModelId")]
    public string WakeWordModelId { get; set; } = string.Empty;

    /// <summary>
    /// Gemeinsame Einleitungswörter, die mit dem sprachabhängigen <c>wakeWordName</c>
    /// zu Aktivierungsformen verbunden werden. „okay“ und „hey“ sind angebotene Formen.
    /// „ok“ wird zusätzlich erkannt. Gemeinsame Einträge gelten für alle Sprachprofile.
    /// </summary>
    [JsonPropertyName("commandAnchorLeadIns")]
    public List<CommandAnchorLeadIn> CommandAnchorLeadIns { get; set; } = [];

    /// <summary>
    /// Aktivierungswortprofile pro Sprache. Die vollständigen Aktivierungsformen entstehen
    /// aus <c>wakeWordName</c> und <c>commandAnchorLeadIns</c>. Der Referenzvertrag verwendet
    /// überall „alex“, erlaubt aber weiterhin sprachabhängige Namen.
    /// </summary>
    [JsonPropertyName("activationProfilesByLanguage")]
    public List<WakeWordLanguageProfile> ActivationProfilesByLanguage { get; set; } = [];

    /// <summary>
    /// Optionale Rückmeldung bei erkannter Äußerung ohne passende Aktivierungsform.
    /// </summary>
    [JsonPropertyName("recoveryFeedback")]
    public WakeWordRecoveryFeedback? RecoveryFeedback { get; set; }

    /// <summary>
    /// Erforderliche Einstellungen für Mikrofonausfälle während der gesamten aktiven Sitzung.
    /// Sie sind getrennt von Hinweisen vor der ersten erfolgreichen Aktivierung modelliert.
    /// </summary>
    [JsonPropertyName("microphoneAvailabilityPolicy")]
    public MicrophoneAvailabilityPolicy MicrophoneAvailabilityPolicy { get; set; } = new();

    /// <summary>
    /// Optionale Rückmeldung nach erfolgreicher Aktivierung.
    /// </summary>
    [JsonPropertyName("activationFeedback")]
    public WakeWordActivationFeedback? ActivationFeedback { get; set; }

    /// <summary>
    /// Optionales Zeitfenster zum Ergänzen einer unvollständigen Äußerung
    /// mit Aktivierungswort durch ein folgendes Transkript.
    /// </summary>
    [JsonPropertyName("wakeUtteranceWindow")]
    public WakeUtteranceWindow? WakeUtteranceWindow { get; set; }
}
