using Microsoft.Extensions.Logging;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.WakeWord.ClientInfrastructure.Services;

/// <summary>
/// Ordnet akustische Detektorereignisse der Assistentenspur oder dem Logger zu,
/// damit Aktivierung und Detektorprobleme mit ihrer jeweiligen Phase diagnostizierbar bleiben.
/// </summary>
internal sealed class WakeWordAcousticDetectionTelemetryForwarder
{
    private readonly ISpeechAssistantTrace _trace;
    private readonly ILogger _logger;
    private readonly SpeechAssistantTraceScope _wakeWordEffectScope;

    public WakeWordAcousticDetectionTelemetryForwarder(
        ISpeechAssistantTrace trace,
        ILogger logger,
        SpeechAssistantTraceScope wakeWordEffectScope)
    {
        _trace = trace ?? throw new ArgumentNullException(nameof(trace));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _wakeWordEffectScope = wakeWordEffectScope;
    }

    public ValueTask RecordAsync(WakeWordAcousticDetectionEvent telemetryEvent, CancellationToken cancellationToken)
    {
        switch (telemetryEvent.Phase)
        {
            case WakeWordAcousticDetectionPhase.Detected:
                return _trace.WriteIfConfiguredAsync(
                    telemetryEvent.CorrelationId,
                    _wakeWordEffectScope,
                    SpeechAssistantTraceLevel.Information,
                    $"Wake word detected via acoustic detector '{telemetryEvent.DetectorId}'.",
                    cancellationToken);
            case WakeWordAcousticDetectionPhase.DetectorUnavailable:
                _logger.LogDebug(telemetryEvent.Exception, "Acoustic wake-word detector unavailable. Falling back to lexical activation.");
                return ValueTask.CompletedTask;
            case WakeWordAcousticDetectionPhase.DetectorWarmup:
                _logger.LogDebug(telemetryEvent.Exception, "Acoustic wake-word detector still warming up. Falling back to lexical activation.");
                return ValueTask.CompletedTask;
            case WakeWordAcousticDetectionPhase.DetectorFailure:
                _logger.LogDebug(telemetryEvent.Exception, "Acoustic wake-word detection failed. Falling back to lexical activation.");
                return ValueTask.CompletedTask;
            default:
                return ValueTask.CompletedTask;
        }
    }
}
