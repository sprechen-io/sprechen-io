using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.WakeWord.ClientInfrastructure.Services;

/// <summary>
/// Formatiert erfolgreiche lexikalische Aktivierungen für die Assistentenspur.
/// Sie zeigt, ob ein Befehl anschließt, sowie Textlängen und Konfidenz.
/// </summary>
internal sealed class WakeWordActivationTelemetryForwarder
{
    private readonly ISpeechAssistantTrace _trace;
    private readonly WakeWordTelemetryValueFormatter _valueFormatter;
    private readonly SpeechAssistantTraceScope _wakeWordEffectScope;
    private readonly SpeechAssistantTraceScope _postWakeRecoveryScope;

    public WakeWordActivationTelemetryForwarder(
        ISpeechAssistantTrace trace,
        WakeWordTelemetryValueFormatter valueFormatter,
        SpeechAssistantTraceScope wakeWordEffectScope,
        SpeechAssistantTraceScope postWakeRecoveryScope)
    {
        _trace = trace ?? throw new ArgumentNullException(nameof(trace));
        _valueFormatter = valueFormatter ?? throw new ArgumentNullException(nameof(valueFormatter));
        _wakeWordEffectScope = wakeWordEffectScope;
        _postWakeRecoveryScope = postWakeRecoveryScope;
    }

    public ValueTask RecordAsync(WakeWordActivationEvent telemetryEvent, CancellationToken cancellationToken)
    {
        return telemetryEvent.Phase switch
        {
            WakeWordActivationPhase.LexicalDetected => _trace.WriteIfConfiguredAsync(
                telemetryEvent.CorrelationId,
                _wakeWordEffectScope,
                SpeechAssistantTraceLevel.Information,
                telemetryEvent.CommandTextPresent
                    ? "Wake word detected with inline command."
                    : "Wake word detected without inline command.",
                cancellationToken),
            WakeWordActivationPhase.Diagnostic => _trace.WriteIfConfiguredAsync(
                telemetryEvent.CorrelationId,
                _postWakeRecoveryScope,
                SpeechAssistantTraceLevel.Information,
                $"wake activation transcriptLen={telemetryEvent.TranscriptLength} commandTextLen={telemetryEvent.CommandTextLength} commandTextPresent={telemetryEvent.CommandTextPresent} confidence={_valueFormatter.FormatConfidence(telemetryEvent.ConfidencePercent)}",
                cancellationToken),
            _ => ValueTask.CompletedTask,
        };
    }
}
