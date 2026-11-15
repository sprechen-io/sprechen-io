namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Protokolliert einen Zustandswechsel vor der Übernahme des neuen Telemetriezustands, damit beide Schritte in festgelegter Reihenfolge erfolgen.
/// </summary>
internal sealed class FormVoiceCommandTelemetryStateEmitter
{
    private readonly IEvaluationSessionTelemetry _telemetry;
    private readonly EvaluationSession _session;
    private string _currentTelemetryState = DialogStates.Init;

    public FormVoiceCommandTelemetryStateEmitter(IEvaluationSessionTelemetry telemetry, EvaluationSession session)
    {
        _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    internal string CurrentTelemetryState => _currentTelemetryState;

    internal void EmitProjectedMicroState(string? microState)
    {
        if (DialogStateTelemetryProjection.TryProjectMicroStateToTelemetryState(microState, out var telemetryState))
        {
            Emit(telemetryState);
        }
    }

    internal void Emit(string toState)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toState);

        _telemetry.TrackStateEntry(_session, toState);
        _currentTelemetryState = toState;
    }
}
