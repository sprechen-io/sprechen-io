namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Bündelt Phase, Aktivierungsstatus, Erkennungsmaße und optionale Diagnose,
/// damit Rückmeldung und Fehler demselben Ablauf zugeordnet bleiben.
/// </summary>
public sealed record WakeWordMissEvent(
    string CorrelationId,
    WakeWordMissPhase Phase,
    WakeWordActivationStatus Status,
    int? ConfidencePercent,
    int? TranscriptLength,
    int? ActivationFormCount,
    int? RecognitionOnlyActivationFormCount,
    Exception? Exception,
    WakeWordMissDiagnosticDetail? Diagnostic);
