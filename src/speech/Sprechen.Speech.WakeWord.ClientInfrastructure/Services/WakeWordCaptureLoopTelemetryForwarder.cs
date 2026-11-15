using Microsoft.Extensions.Logging;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.WakeWord.ClientInfrastructure.Services;

/// <summary>
/// Protokolliert Aufnahme- und Erkennungswiederholungen der Aktivierungsschleife,
/// damit fehlende Sprache und technische Fehler unterschiedliche Diagnosen behalten.
/// </summary>
internal sealed class WakeWordCaptureLoopTelemetryForwarder
{
    private readonly ISpeechAssistantTrace _trace;
    private readonly ILogger _logger;
    private readonly SpeechAssistantTraceScope _wakeWordEffectScope;

    public WakeWordCaptureLoopTelemetryForwarder(
        ISpeechAssistantTrace trace,
        ILogger logger,
        SpeechAssistantTraceScope wakeWordEffectScope)
    {
        _trace = trace ?? throw new ArgumentNullException(nameof(trace));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _wakeWordEffectScope = wakeWordEffectScope;
    }

    public ValueTask RecordAsync(WakeWordCaptureLoopEvent telemetryEvent, CancellationToken cancellationToken)
    {
        switch (telemetryEvent.Phase)
        {
            case WakeWordCaptureLoopPhase.CaptureNoSpeechRetry:
                return _trace.WriteIfConfiguredAsync(
                    telemetryEvent.CorrelationId,
                    _wakeWordEffectScope,
                    SpeechAssistantTraceLevel.Information,
                    "Wake-word capture completed without speech and will retry.",
                    cancellationToken);
            case WakeWordCaptureLoopPhase.CaptureFailedRetry:
                _logger.LogDebug(telemetryEvent.Exception, "Wake word capture failed.");
                // Ausnahmetyp und Nachricht gehören auch in die Warnspur, damit eine ständig
                // fehlschlagende Aufnahme bei unterdrückten Debuglogs einen untersuchbaren Grund
                // statt nur der angekündigten Wiederholung hinterlässt.
                return _trace.WriteIfConfiguredAsync(
                    telemetryEvent.CorrelationId,
                    _wakeWordEffectScope,
                    SpeechAssistantTraceLevel.Warning,
                    FormatCaptureFailedMessage(telemetryEvent.Exception),
                    cancellationToken);
            case WakeWordCaptureLoopPhase.TranscriptionNoSpeechRetry:
                return _trace.WriteIfConfiguredAsync(
                    telemetryEvent.CorrelationId,
                    _wakeWordEffectScope,
                    SpeechAssistantTraceLevel.Information,
                    "Wake-word transcription returned no speech and the listener will retry.",
                    cancellationToken);
            case WakeWordCaptureLoopPhase.TranscriptionEmptyRetry:
                return _trace.WriteIfConfiguredAsync(
                    telemetryEvent.CorrelationId,
                    _wakeWordEffectScope,
                    SpeechAssistantTraceLevel.Information,
                    "Wake-word transcription returned an empty result and the listener will retry.",
                    cancellationToken);
            default:
                return ValueTask.CompletedTask;
        }
    }

    private static string FormatCaptureFailedMessage(Exception? exception)
    {
        const string baseMessage = "Wake-word capture failed and the listener will retry.";
        if (exception is null)
        {
            return baseMessage;
        }

        var message = exception.Message;
        return string.IsNullOrWhiteSpace(message)
            ? $"{baseMessage} reason={exception.GetType().Name}"
            : $"{baseMessage} reason={exception.GetType().Name}: {message}";
    }
}
