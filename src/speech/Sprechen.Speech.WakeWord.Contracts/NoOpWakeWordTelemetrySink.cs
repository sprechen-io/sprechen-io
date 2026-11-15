namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Verwirft Ereignisse nach Prüfung ihrer Phase, damit Tests den Telemetrievertrag
/// ohne Ausgabe verwenden können. Für Tests und noch nicht angebundene Instanzen
/// vorgesehen. Kein Ersatz für die Telemetrieanbindung im Produktivbetrieb.
/// </summary>
public sealed class NoOpWakeWordTelemetrySink : IWakeWordTelemetrySink
{
    public static readonly NoOpWakeWordTelemetrySink Instance = new();

    public ValueTask RecordListenerSetupAsync(WakeWordListenerSetupEvent telemetryEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(telemetryEvent);
        _ = WakeWordTelemetryEventCodes.ToEventCode(telemetryEvent.Phase);
        return ValueTask.CompletedTask;
    }

    public ValueTask RecordAcousticDetectionAsync(WakeWordAcousticDetectionEvent telemetryEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(telemetryEvent);
        _ = WakeWordTelemetryEventCodes.ToEventCode(telemetryEvent.Phase);
        return ValueTask.CompletedTask;
    }

    public ValueTask RecordCaptureLoopAsync(WakeWordCaptureLoopEvent telemetryEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(telemetryEvent);
        _ = WakeWordTelemetryEventCodes.ToEventCode(telemetryEvent.Phase);
        return ValueTask.CompletedTask;
    }

    public ValueTask RecordTranscriptionRejectionAsync(WakeWordTranscriptionRejectionEvent telemetryEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(telemetryEvent);
        _ = WakeWordTelemetryEventCodes.ToEventCode(telemetryEvent.Phase);
        return ValueTask.CompletedTask;
    }

    public ValueTask RecordStitchAsync(WakeWordStitchEvent telemetryEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(telemetryEvent);
        _ = WakeWordTelemetryEventCodes.ToEventCode(telemetryEvent.Phase);
        return ValueTask.CompletedTask;
    }

    public ValueTask RecordMissAsync(WakeWordMissEvent telemetryEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(telemetryEvent);
        _ = WakeWordTelemetryEventCodes.ToEventCode(telemetryEvent.Phase);
        return ValueTask.CompletedTask;
    }

    public ValueTask RecordActivationAsync(WakeWordActivationEvent telemetryEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(telemetryEvent);
        _ = WakeWordTelemetryEventCodes.ToEventCode(telemetryEvent.Phase);
        return ValueTask.CompletedTask;
    }

    public ValueTask RecordFallbackAsync(WakeWordFallbackEvent telemetryEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(telemetryEvent);
        _ = WakeWordTelemetryEventCodes.ToEventCode(telemetryEvent.Phase);
        return ValueTask.CompletedTask;
    }
}
