using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.Speech.WakeWord.Contracts;

/// <summary>
/// Ordnet Aktivierungsphasen den stabilen Ereigniscodes des Modells zu, damit
/// Laufzeit und Prüfungen dasselbe Vokabular verwenden. Unbekannte Phasen werden abgewiesen.
/// </summary>
public static class WakeWordTelemetryEventCodes
{
    public static string ToEventCode(WakeWordListenerSetupPhase phase) => phase switch
    {
        WakeWordListenerSetupPhase.Started => TelemetryWakeWordListenerSetupEventCodes.WakeWordListenerStarted,
        WakeWordListenerSetupPhase.InventoryLoaded => TelemetryWakeWordListenerSetupEventCodes.WakeWordListenerInventoryLoaded,
        WakeWordListenerSetupPhase.Failed => TelemetryWakeWordListenerSetupEventCodes.WakeWordListenerSetupFailed,
        _ => throw UnknownPhase(nameof(phase), phase),
    };

    public static string ToEventCode(WakeWordAcousticDetectionPhase phase) => phase switch
    {
        WakeWordAcousticDetectionPhase.Detected => TelemetryWakeWordAcousticDetectionEventCodes.WakeWordAcousticDetected,
        WakeWordAcousticDetectionPhase.DetectorUnavailable => TelemetryWakeWordAcousticDetectionEventCodes.WakeWordAcousticDetectorUnavailable,
        WakeWordAcousticDetectionPhase.DetectorWarmup => TelemetryWakeWordAcousticDetectionEventCodes.WakeWordAcousticDetectorWarmup,
        WakeWordAcousticDetectionPhase.DetectorFailure => TelemetryWakeWordAcousticDetectionEventCodes.WakeWordAcousticDetectorFailure,
        _ => throw UnknownPhase(nameof(phase), phase),
    };

    public static string ToEventCode(WakeWordCaptureLoopPhase phase) => phase switch
    {
        WakeWordCaptureLoopPhase.CaptureNoSpeechRetry => TelemetryWakeWordCaptureLoopEventCodes.WakeWordCaptureNoSpeechRetry,
        WakeWordCaptureLoopPhase.CaptureFailedRetry => TelemetryWakeWordCaptureLoopEventCodes.WakeWordCaptureFailedRetry,
        WakeWordCaptureLoopPhase.TranscriptionNoSpeechRetry => TelemetryWakeWordCaptureLoopEventCodes.WakeWordTranscriptionNoSpeechRetry,
        WakeWordCaptureLoopPhase.TranscriptionEmptyRetry => TelemetryWakeWordCaptureLoopEventCodes.WakeWordTranscriptionEmptyRetry,
        _ => throw UnknownPhase(nameof(phase), phase),
    };

    public static string ToEventCode(WakeWordTranscriptionRejectionPhase phase) => phase switch
    {
        WakeWordTranscriptionRejectionPhase.QueueFull => TelemetryWakeWordTranscriptionRejectionEventCodes.WakeWordTranscriptionQueueFull,
        WakeWordTranscriptionRejectionPhase.BackoffExhausted => TelemetryWakeWordTranscriptionRejectionEventCodes.WakeWordTranscriptionBackoffExhausted,
        WakeWordTranscriptionRejectionPhase.MicGateHardOff => TelemetryWakeWordTranscriptionRejectionEventCodes.WakeWordTranscriptionMicGateHardOff,
        WakeWordTranscriptionRejectionPhase.Dispatched => TelemetryWakeWordTranscriptionRejectionEventCodes.WakeWordTranscriptionDispatched,
        _ => throw UnknownPhase(nameof(phase), phase),
    };

    public static string ToEventCode(WakeWordStitchPhase phase) => phase switch
    {
        WakeWordStitchPhase.Started => TelemetryWakeStitchEventCodes.WakeStitchStarted,
        WakeWordStitchPhase.Attached => TelemetryWakeStitchEventCodes.WakeStitchAttached,
        WakeWordStitchPhase.Expired => TelemetryWakeStitchEventCodes.WakeStitchExpired,
        WakeWordStitchPhase.NoOp => TelemetryWakeStitchEventCodes.WakeStitchNoOp,
        _ => throw UnknownPhase(nameof(phase), phase),
    };

    public static string ToEventCode(WakeWordMissPhase phase) => phase switch
    {
        WakeWordMissPhase.MissTrace => TelemetryWakeWordMissEventCodes.WakeWordMissTrace,
        WakeWordMissPhase.MissDiagnostic => TelemetryWakeWordMissEventCodes.WakeWordMissDiagnostic,
        WakeWordMissPhase.EarconFailed => TelemetryWakeWordMissEventCodes.WakeWordMissEarconFailed,
        WakeWordMissPhase.RecordingCleanupFailed => TelemetryWakeWordMissEventCodes.WakeWordMissRecordingCleanupFailed,
        WakeWordMissPhase.GuidanceSpoken => TelemetryWakeWordMissEventCodes.WakeWordMissGuidanceSpoken,
        _ => throw UnknownPhase(nameof(phase), phase),
    };

    /// <summary>
    /// Ordnet eine <see cref="WakeWordMissPhase"/> ihrem modellierten Telemetriewert zu.
    /// Die explizite Zuordnung verhindert, dass eine Umbenennung des Enum-Mitglieds
    /// über <c>ToString()</c> unbemerkt das übertragene Vokabular verändert.
    /// </summary>
    public static string ToPhaseValue(WakeWordMissPhase phase) => phase switch
    {
        WakeWordMissPhase.MissTrace => TelemetryWakeWordMissPhases.MissTrace,
        WakeWordMissPhase.MissDiagnostic => TelemetryWakeWordMissPhases.MissDiagnostic,
        WakeWordMissPhase.EarconFailed => TelemetryWakeWordMissPhases.EarconFailed,
        WakeWordMissPhase.RecordingCleanupFailed => TelemetryWakeWordMissPhases.RecordingCleanupFailed,
        WakeWordMissPhase.GuidanceSpoken => TelemetryWakeWordMissPhases.GuidanceSpoken,
        _ => throw UnknownPhase(nameof(phase), phase),
    };

    /// <summary>
    /// Ordnet den Aktivierungsstatus seinem Modellwert zu, damit Umbenennungen
    /// wie bei <see cref="ToPhaseValue(WakeWordMissPhase)"/> die Telemetrie nicht verändern.
    /// </summary>
    public static string ToStatusValue(WakeWordActivationStatus status) => status switch
    {
        WakeWordActivationStatus.NoRecognizedSpeech => TelemetryWakeWordActivationStatuses.NoRecognizedSpeech,
        WakeWordActivationStatus.MissingLeadingWakeWord => TelemetryWakeWordActivationStatuses.MissingLeadingWakeWord,
        WakeWordActivationStatus.LeadingWakeWordDetected => TelemetryWakeWordActivationStatuses.LeadingWakeWordDetected,
        _ => throw UnknownPhase(nameof(status), status),
    };

    public static string ToEventCode(WakeWordActivationPhase phase) => phase switch
    {
        WakeWordActivationPhase.LexicalDetected => TelemetryWakeWordActivationEventCodes.WakeWordActivationLexicalDetected,
        WakeWordActivationPhase.Diagnostic => TelemetryWakeWordActivationEventCodes.WakeWordActivationDiagnostic,
        _ => throw UnknownPhase(nameof(phase), phase),
    };

    public static string ToEventCode(WakeWordFallbackPhase phase) => phase switch
    {
        WakeWordFallbackPhase.Published => TelemetryWakeWordFallbackEventCodes.WakeWordFallbackPublished,
        WakeWordFallbackPhase.PublishFailed => TelemetryWakeWordFallbackEventCodes.WakeWordFallbackPublishFailed,
        _ => throw UnknownPhase(nameof(phase), phase),
    };

    private static ArgumentOutOfRangeException UnknownPhase<TPhase>(string parameterName, TPhase phase)
        where TPhase : struct, Enum
    {
        return new ArgumentOutOfRangeException(
            parameterName,
            phase,
            $"Unknown wake-word telemetry phase '{typeof(TPhase).Name}.{phase}'.");
    }
}
