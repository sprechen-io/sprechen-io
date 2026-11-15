namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Nimmt die Antwort auf eine Feldbestätigung auf und wertet sie aus.
/// Fehlt ein Transkript oder lässt es sich nicht zuordnen, wird eine
/// unentschiedene Antwort mit passendem Grund zurückgegeben.
/// </summary>
internal sealed class FormVoiceCommandConfirmationDecisionService
{
    private readonly FormVoiceCommandConfirmationTranscriptCapture _transcriptCapture;
    private readonly FormVoiceCommandConfirmationTranscriptInterpreter _transcriptInterpreter;
    private readonly FormVoiceCommandConfirmationMissingDecisionPolicy _missingDecisionPolicy;

    public FormVoiceCommandConfirmationDecisionService(
        FormVoiceCommandConfirmationTranscriptCapture transcriptCapture,
        FormVoiceCommandConfirmationTranscriptInterpreter transcriptInterpreter,
        FormVoiceCommandConfirmationMissingDecisionPolicy missingDecisionPolicy)
    {
        _transcriptCapture = transcriptCapture ?? throw new ArgumentNullException(nameof(transcriptCapture));
        _transcriptInterpreter = transcriptInterpreter ?? throw new ArgumentNullException(nameof(transcriptInterpreter));
        _missingDecisionPolicy = missingDecisionPolicy ?? throw new ArgumentNullException(nameof(missingDecisionPolicy));
    }

    public async Task<ConfirmationDecision> CaptureDecisionAsync(
        FormVoiceCommandContext context,
        EvaluationSession session,
        FormVoiceCommandStateTracker stateTracker,
        string prompt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var decisionTranscript = await _transcriptCapture.CaptureAsync(
                context,
                session,
                stateTracker,
                prompt,
                cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(decisionTranscript))
        {
            return _missingDecisionPolicy.Resolve(hasTranscript: false);
        }

        return _transcriptInterpreter.TryInterpret(context, decisionTranscript)
            ?? _missingDecisionPolicy.Resolve(hasTranscript: true);
    }
}
