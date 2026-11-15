using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.Speech.WakeWord.ClientInfrastructure.Services;

/// <summary>
/// Protokolliert Beginn, Zusammenfügen und Ablauf übernommener Aktivierungspräfixe,
/// damit die Diagnose mehrere Aufnahmezyklen ohne deren Transkript verfolgen kann.
/// </summary>
internal sealed class WakeWordStitchTelemetryForwarder
{
    private readonly ISpeechAssistantTrace _trace;
    private readonly SpeechAssistantTraceScope _postWakeRecoveryScope;

    public WakeWordStitchTelemetryForwarder(
        ISpeechAssistantTrace trace,
        SpeechAssistantTraceScope postWakeRecoveryScope)
    {
        _trace = trace ?? throw new ArgumentNullException(nameof(trace));
        _postWakeRecoveryScope = postWakeRecoveryScope;
    }

    public ValueTask RecordAsync(WakeWordStitchEvent telemetryEvent, CancellationToken cancellationToken)
    {
        return telemetryEvent.Phase switch
        {
            WakeWordStitchPhase.Attached => _trace.WriteIfConfiguredAsync(
                telemetryEvent.CorrelationId,
                _postWakeRecoveryScope,
                SpeechAssistantTraceLevel.Information,
                $"wake stitched stitchedLen={telemetryEvent.StitchedTranscriptLength} commandTextLen={telemetryEvent.CommandTextLength}",
                cancellationToken),
            WakeWordStitchPhase.Started => _trace.WriteIfConfiguredAsync(
                telemetryEvent.CorrelationId,
                _postWakeRecoveryScope,
                SpeechAssistantTraceLevel.Information,
                $"wake carried prefixLen={telemetryEvent.TranscriptTextLength}",
                cancellationToken),
            WakeWordStitchPhase.Expired => _trace.WriteIfConfiguredAsync(
                telemetryEvent.CorrelationId,
                _postWakeRecoveryScope,
                SpeechAssistantTraceLevel.Information,
                $"wake stitch expired previousPrefixLen={telemetryEvent.TranscriptTextLength}",
                cancellationToken),
            WakeWordStitchPhase.NoOp => _trace.WriteIfConfiguredAsync(
                telemetryEvent.CorrelationId,
                _postWakeRecoveryScope,
                SpeechAssistantTraceLevel.Information,
                $"wake stitch noop transcriptLen={telemetryEvent.TranscriptTextLength}",
                cancellationToken),
            _ => ValueTask.CompletedTask,
        };
    }
}
