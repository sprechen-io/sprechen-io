using Microsoft.Extensions.Logging;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.WakeWord.ClientInfrastructure.Services;

/// <summary>
/// Verteilt typisierte Aktivierungsereignisse an ihre phasenspezifischen Diagnoseadapter,
/// damit Spur- und Logausgabe einheitlich über einen Ereigniszugang erreichbar bleiben.
/// </summary>
internal sealed class WakeWordTelemetryForwardingSink : IWakeWordTelemetrySink
{
    private readonly WakeWordListenerSetupTelemetryForwarder _listenerSetup;
    private readonly WakeWordAcousticDetectionTelemetryForwarder _acousticDetection;
    private readonly WakeWordCaptureLoopTelemetryForwarder _captureLoop;
    private readonly WakeWordTranscriptionRejectionTelemetryForwarder _transcriptionRejection;
    private readonly WakeWordStitchTelemetryForwarder _stitch;
    private readonly WakeWordMissTelemetryForwarder _miss;
    private readonly WakeWordActivationTelemetryForwarder _activation;
    private readonly WakeWordFallbackTelemetryForwarder _fallback;

    public WakeWordTelemetryForwardingSink(
        ISpeechAssistantTrace trace,
        ILogger<WakeWordTelemetryForwardingSink> logger)
    {
        ArgumentNullException.ThrowIfNull(trace);
        ArgumentNullException.ThrowIfNull(logger);

        var wakeWordEffectScope = SpeechAssistantTraceScope.WakeWordEffect;
        var postWakeRecoveryScope = SpeechAssistantTraceScope.PostWakeWordRecovery;
        var valueFormatter = new WakeWordTelemetryValueFormatter();

        _listenerSetup = new WakeWordListenerSetupTelemetryForwarder(trace, logger, wakeWordEffectScope);
        _acousticDetection = new WakeWordAcousticDetectionTelemetryForwarder(trace, logger, wakeWordEffectScope);
        _captureLoop = new WakeWordCaptureLoopTelemetryForwarder(trace, logger, wakeWordEffectScope);
        _transcriptionRejection = new WakeWordTranscriptionRejectionTelemetryForwarder(trace, logger, wakeWordEffectScope);
        _stitch = new WakeWordStitchTelemetryForwarder(trace, postWakeRecoveryScope);
        var missActionFactory = new WakeWordMissTelemetryActionFactory(valueFormatter, wakeWordEffectScope, postWakeRecoveryScope);

        _miss = new WakeWordMissTelemetryForwarder(trace, logger, missActionFactory);
        _activation = new WakeWordActivationTelemetryForwarder(trace, valueFormatter, wakeWordEffectScope, postWakeRecoveryScope);
        _fallback = new WakeWordFallbackTelemetryForwarder(trace, logger, wakeWordEffectScope);
    }

    public ValueTask RecordListenerSetupAsync(WakeWordListenerSetupEvent telemetryEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(telemetryEvent);
        _ = WakeWordTelemetryEventCodes.ToEventCode(telemetryEvent.Phase);
        return _listenerSetup.RecordAsync(telemetryEvent, cancellationToken);
    }

    public ValueTask RecordAcousticDetectionAsync(WakeWordAcousticDetectionEvent telemetryEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(telemetryEvent);
        _ = WakeWordTelemetryEventCodes.ToEventCode(telemetryEvent.Phase);
        return _acousticDetection.RecordAsync(telemetryEvent, cancellationToken);
    }

    public ValueTask RecordCaptureLoopAsync(WakeWordCaptureLoopEvent telemetryEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(telemetryEvent);
        _ = WakeWordTelemetryEventCodes.ToEventCode(telemetryEvent.Phase);
        return _captureLoop.RecordAsync(telemetryEvent, cancellationToken);
    }

    public ValueTask RecordTranscriptionRejectionAsync(WakeWordTranscriptionRejectionEvent telemetryEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(telemetryEvent);
        _ = WakeWordTelemetryEventCodes.ToEventCode(telemetryEvent.Phase);
        return _transcriptionRejection.RecordAsync(telemetryEvent, cancellationToken);
    }

    public ValueTask RecordStitchAsync(WakeWordStitchEvent telemetryEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(telemetryEvent);
        _ = WakeWordTelemetryEventCodes.ToEventCode(telemetryEvent.Phase);
        return _stitch.RecordAsync(telemetryEvent, cancellationToken);
    }

    public ValueTask RecordMissAsync(WakeWordMissEvent telemetryEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(telemetryEvent);
        _ = WakeWordTelemetryEventCodes.ToEventCode(telemetryEvent.Phase);
        return _miss.RecordAsync(telemetryEvent, cancellationToken);
    }

    public ValueTask RecordActivationAsync(WakeWordActivationEvent telemetryEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(telemetryEvent);
        _ = WakeWordTelemetryEventCodes.ToEventCode(telemetryEvent.Phase);
        return _activation.RecordAsync(telemetryEvent, cancellationToken);
    }

    public ValueTask RecordFallbackAsync(WakeWordFallbackEvent telemetryEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(telemetryEvent);
        _ = WakeWordTelemetryEventCodes.ToEventCode(telemetryEvent.Phase);
        return _fallback.RecordAsync(telemetryEvent, cancellationToken);
    }
}
