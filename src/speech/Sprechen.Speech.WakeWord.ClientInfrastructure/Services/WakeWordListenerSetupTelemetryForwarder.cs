using Microsoft.Extensions.Logging;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.WakeWord.ClientInfrastructure.Services;

/// <summary>
/// Protokolliert Start, geladenes Inventar und Einrichtungsfehler der Aktivierungsschleife,
/// damit deren Ausgangsbedingungen für die spätere Diagnose verfügbar sind.
/// </summary>
internal sealed class WakeWordListenerSetupTelemetryForwarder
{
    private readonly ISpeechAssistantTrace _trace;
    private readonly ILogger _logger;
    private readonly SpeechAssistantTraceScope _wakeWordEffectScope;

    public WakeWordListenerSetupTelemetryForwarder(
        ISpeechAssistantTrace trace,
        ILogger logger,
        SpeechAssistantTraceScope wakeWordEffectScope)
    {
        _trace = trace ?? throw new ArgumentNullException(nameof(trace));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _wakeWordEffectScope = wakeWordEffectScope;
    }

    public ValueTask RecordAsync(WakeWordListenerSetupEvent telemetryEvent, CancellationToken cancellationToken)
    {
        return telemetryEvent.Phase switch
        {
            WakeWordListenerSetupPhase.Started => _trace.WriteIfConfiguredAsync(
                telemetryEvent.CorrelationId,
                _wakeWordEffectScope,
                SpeechAssistantTraceLevel.Information,
                $"Wake-word listener started for languageCode={telemetryEvent.IsoCode}.",
                cancellationToken),
            WakeWordListenerSetupPhase.InventoryLoaded => _trace.WriteIfConfiguredAsync(
                telemetryEvent.CorrelationId,
                _wakeWordEffectScope,
                SpeechAssistantTraceLevel.Information,
                $"Wake-word inventory loaded. activationFormCount={telemetryEvent.ActivationFormCount} recognitionOnlyActivationFormCount={telemetryEvent.RecognitionOnlyActivationFormCount}",
                cancellationToken),
            WakeWordListenerSetupPhase.Failed => RecordFailedAsync(telemetryEvent, cancellationToken),
            _ => ValueTask.CompletedTask,
        };
    }

    private ValueTask RecordFailedAsync(WakeWordListenerSetupEvent telemetryEvent, CancellationToken cancellationToken)
    {
        _logger.LogDebug(telemetryEvent.Exception, "Wake word listener setup failed.");
        return _trace.WriteIfConfiguredAsync(
            telemetryEvent.CorrelationId,
            _wakeWordEffectScope,
            SpeechAssistantTraceLevel.Error,
            "Wake-word listener setup failed.",
            cancellationToken);
    }
}
