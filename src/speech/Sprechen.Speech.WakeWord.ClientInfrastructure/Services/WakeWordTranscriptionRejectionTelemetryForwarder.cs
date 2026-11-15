using Microsoft.Extensions.Logging;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.WakeWord.ClientInfrastructure.Services;

/// <summary>
/// Ordnet abgelehnte Erkennungsversuche ihren Diagnoseausgaben zu,
/// damit Warteschlangengrenzen, ausgeschöpfte Wiederholungen und Mikrofonsperren unterscheidbar bleiben.
/// </summary>
internal sealed class WakeWordTranscriptionRejectionTelemetryForwarder
{
    private readonly ISpeechAssistantTrace _trace;
    private readonly ILogger _logger;
    private readonly SpeechAssistantTraceScope _wakeWordEffectScope;

    public WakeWordTranscriptionRejectionTelemetryForwarder(
        ISpeechAssistantTrace trace,
        ILogger logger,
        SpeechAssistantTraceScope wakeWordEffectScope)
    {
        _trace = trace ?? throw new ArgumentNullException(nameof(trace));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _wakeWordEffectScope = wakeWordEffectScope;
    }

    public ValueTask RecordAsync(WakeWordTranscriptionRejectionEvent telemetryEvent, CancellationToken cancellationToken)
    {
        switch (telemetryEvent.Phase)
        {
            case WakeWordTranscriptionRejectionPhase.QueueFull:
                _logger.LogDebug(telemetryEvent.Exception, "Wake word transcription rejected: queue full.");
                return ValueTask.CompletedTask;
            case WakeWordTranscriptionRejectionPhase.BackoffExhausted:
                _logger.LogDebug(telemetryEvent.Exception, "Wake word transcription rejected: backoff exhausted.");
                return ValueTask.CompletedTask;
            case WakeWordTranscriptionRejectionPhase.MicGateHardOff:
                _logger.LogDebug(telemetryEvent.Exception, "Wake word transcription rejected: mic gate hard-off.");
                return ValueTask.CompletedTask;
            case WakeWordTranscriptionRejectionPhase.Dispatched:
                return _trace.WriteIfConfiguredAsync(
                    telemetryEvent.CorrelationId,
                    _wakeWordEffectScope,
                    SpeechAssistantTraceLevel.Warning,
                    $"Wake-word transcription rejected by worker (code={telemetryEvent.FailureCode}); re-arming wake listener.",
                    cancellationToken);
            default:
                return ValueTask.CompletedTask;
        }
    }
}
