namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Verknüpft den Aufbau der Aktivierungserkennung mit Sprache und Bestandsgrößen, damit Konfigurationsfehler einem Ablauf zugeordnet werden können.
/// </summary>
public sealed record WakeWordListenerSetupEvent(
    string CorrelationId,
    WakeWordListenerSetupPhase Phase,
    string? IsoCode,
    int? ActivationFormCount,
    int? RecognitionOnlyActivationFormCount,
    Exception? Exception);
