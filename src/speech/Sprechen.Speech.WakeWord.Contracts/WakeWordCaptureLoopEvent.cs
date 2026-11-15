namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Ordnet eine Wiederholung samt möglichem Fehler dem Erkennungsablauf zu, damit Aufnahme- und Transkriptionsprobleme rückverfolgbar bleiben.
/// </summary>
public sealed record WakeWordCaptureLoopEvent(
    string CorrelationId,
    WakeWordCaptureLoopPhase Phase,
    Exception? Exception);
