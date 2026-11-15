namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Fordert die gemeinsame Rückmeldung für unverwendbare Zifferneingabe mit der passenden Reparaturkennung an. Die nächste Aufnahme bleibt beim Treiber.
/// </summary>
internal sealed class DigitsWorkflowInputRejectionFlow
{
    private readonly WorkflowUnrecognizedInputAnnouncer _announcer;

    public DigitsWorkflowInputRejectionFlow(WorkflowUnrecognizedInputAnnouncer announcer)
    {
        _announcer = announcer ?? throw new ArgumentNullException(nameof(announcer));
    }

    /// <summary>
    /// Fordert die gemeinsame Rückmeldung für nicht als Ziffern gelesene Eingabe an
    /// und ergänzt deren Reparaturkennung. Weitere Aufnahmen und der Abschluss bleiben beim Treiber.
    /// </summary>
    public Task HandleAsync(
        FormVoiceCommandContext context,
        FormFieldSnapshot field,
        EvaluationSession session,
        FormVoiceCommandStateTracker stateTracker,
        FormVoiceCommandUnrecognizedInputEscalationPolicy escalation,
        string rejectedTranscript,
        CancellationToken cancellationToken)
        => _announcer.AnnounceAsync(
            context,
            field,
            session,
            stateTracker,
            escalation,
            rejectedTranscript,
            DigitsWorkflowRepairEventCodes.UnrecognizedBlock,
            cancellationToken);
}
