namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Bündelt strukturierte Telemetrie der Aktivierungserkennung als gemeinsame
/// Grundlage für Trace- und Logausgaben. Jede Methode vertritt eine Phasenfamilie,
/// deren Varianten im Ereignisdatensatz stehen. Meldungstexte entstehen bei der Ausgabe.
/// </summary>
public interface IWakeWordTelemetrySink
{
    ValueTask RecordListenerSetupAsync(WakeWordListenerSetupEvent telemetryEvent, CancellationToken cancellationToken);

    ValueTask RecordAcousticDetectionAsync(WakeWordAcousticDetectionEvent telemetryEvent, CancellationToken cancellationToken);

    ValueTask RecordCaptureLoopAsync(WakeWordCaptureLoopEvent telemetryEvent, CancellationToken cancellationToken);

    ValueTask RecordTranscriptionRejectionAsync(WakeWordTranscriptionRejectionEvent telemetryEvent, CancellationToken cancellationToken);

    ValueTask RecordStitchAsync(WakeWordStitchEvent telemetryEvent, CancellationToken cancellationToken);

    ValueTask RecordMissAsync(WakeWordMissEvent telemetryEvent, CancellationToken cancellationToken);

    ValueTask RecordActivationAsync(WakeWordActivationEvent telemetryEvent, CancellationToken cancellationToken);

    ValueTask RecordFallbackAsync(WakeWordFallbackEvent telemetryEvent, CancellationToken cancellationToken);
}
