namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Ordnet eine verworfene Transkription samt Fehlercode dem Erkennungsablauf zu, damit deren Ursache über die Weiterleitung erhalten bleibt.
/// </summary>
public sealed record WakeWordTranscriptionRejectionEvent(
    string CorrelationId,
    WakeWordTranscriptionRejectionPhase Phase,
    string? FailureCode,
    Exception? Exception);
