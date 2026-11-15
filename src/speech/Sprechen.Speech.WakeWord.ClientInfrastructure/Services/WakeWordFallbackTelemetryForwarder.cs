using Microsoft.Extensions.Logging;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.WakeWord.ClientInfrastructure.Services;

/// <summary>
/// Protokolliert den Wechsel zur lexikalischen Aktivierung und Veröffentlichungsfehler,
/// damit die Diagnose den tatsächlich gewählten Erkennungsweg erklären kann.
/// </summary>
internal sealed class WakeWordFallbackTelemetryForwarder
{
    private readonly ISpeechAssistantTrace _trace;
    private readonly ILogger _logger;
    private readonly SpeechAssistantTraceScope _wakeWordEffectScope;

    public WakeWordFallbackTelemetryForwarder(
        ISpeechAssistantTrace trace,
        ILogger logger,
        SpeechAssistantTraceScope wakeWordEffectScope)
    {
        _trace = trace ?? throw new ArgumentNullException(nameof(trace));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _wakeWordEffectScope = wakeWordEffectScope;
    }

    public ValueTask RecordAsync(WakeWordFallbackEvent telemetryEvent, CancellationToken cancellationToken)
    {
        switch (telemetryEvent.Phase)
        {
            case WakeWordFallbackPhase.Published:
                return _trace.WriteIfConfiguredAsync(
                    telemetryEvent.CorrelationId,
                    _wakeWordEffectScope,
                    SpeechAssistantTraceLevel.Information,
                    $"Wake-word listener is using lexical fallback (reasonCode={telemetryEvent.ReasonCode}).",
                    cancellationToken);
            case WakeWordFallbackPhase.PublishFailed:
                _logger.LogDebug(telemetryEvent.Exception, "Failed to publish wake-listening fallback state.");
                return ValueTask.CompletedTask;
            default:
                return ValueTask.CompletedTask;
        }
    }
}
