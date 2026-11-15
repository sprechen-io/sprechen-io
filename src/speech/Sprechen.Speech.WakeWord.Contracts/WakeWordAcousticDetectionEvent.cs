namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Verknüpft Detektorphase und möglichen Fehler mit einem Erkennungsablauf.
/// </summary>
public sealed record WakeWordAcousticDetectionEvent(
    string CorrelationId,
    WakeWordAcousticDetectionPhase Phase,
    string? DetectorId,
    Exception? Exception);
