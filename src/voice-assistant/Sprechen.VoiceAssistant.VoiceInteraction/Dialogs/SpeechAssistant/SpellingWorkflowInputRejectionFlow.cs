namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Fordert die gemeinsame Rückmeldung für eine nicht verstandene Buchstabiereingabe
/// mit deren Reparaturkennung an. Der Treiber übernimmt die nächste Erfassung.
/// </summary>
internal sealed class SpellingWorkflowInputRejectionFlow
{
    private readonly WorkflowUnrecognizedInputAnnouncer _announcer;

    public SpellingWorkflowInputRejectionFlow(WorkflowUnrecognizedInputAnnouncer announcer)
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
            SpellingWorkflowRepairEventCodes.UnrecognizedCharacters,
            cancellationToken);
}
