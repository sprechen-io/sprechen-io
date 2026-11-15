namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Fordert die gemeinsame Rückmeldung für eine nicht verstandene Mengeneingabe
/// mit deren Reparaturkennung an. Erfassung und weiterer Abschluss liegen beim Treiber.
/// </summary>
internal sealed class SetWorkflowInputRejectionFlow
{
    private readonly WorkflowUnrecognizedInputAnnouncer _announcer;

    public SetWorkflowInputRejectionFlow(WorkflowUnrecognizedInputAnnouncer announcer)
    {
        _announcer = announcer ?? throw new ArgumentNullException(nameof(announcer));
    }

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
            SetWorkflowRepairEventCodes.UnrecognizedSelection,
            cancellationToken);
}
