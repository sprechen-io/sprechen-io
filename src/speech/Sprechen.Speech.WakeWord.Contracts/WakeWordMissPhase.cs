namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Unterscheidet Diagnose, Aufnahmebereinigung und hörbare Rückmeldung
/// im Aktivierungsablauf, damit ihre Fehlerursachen getrennt erkennbar bleiben.
/// </summary>
public enum WakeWordMissPhase
{
    MissTrace = 0,
    MissDiagnostic = 1,
    EarconFailed = 2,
    RecordingCleanupFailed = 3,
    GuidanceSpoken = 4
}
