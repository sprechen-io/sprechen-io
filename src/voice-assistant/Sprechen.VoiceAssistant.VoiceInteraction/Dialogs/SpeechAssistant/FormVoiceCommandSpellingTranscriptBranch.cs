namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Leitet Transkripte im Buchstabiermodus an die schrittweise Zeicheneingabe
/// weiter und überführt deren Ergebnis in die weitere Feldverarbeitung.
/// </summary>
internal sealed class FormVoiceCommandSpellingTranscriptBranch
{
    private readonly FormVoiceCommandSpellingWorkflowService _spellingWorkflowService;

    public FormVoiceCommandSpellingTranscriptBranch(FormVoiceCommandSpellingWorkflowService spellingWorkflowService)
    {
        _spellingWorkflowService = spellingWorkflowService ?? throw new ArgumentNullException(nameof(spellingWorkflowService));
    }

    public async Task<FormVoiceCommandFieldOrchestrationResult?> TryHandleAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        FormVoiceCommandTranscriptInterpretation interpretation,
        int? sttConfidencePercent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        ArgumentNullException.ThrowIfNull(interpretation);

        if (!interpretation.IsSpellingMode)
        {
            return null;
        }

        var spellingWorkflowResult = await _spellingWorkflowService.CaptureSpellingTranscriptAsync(
                turnContext.Context,
                turnContext.Interpreter,
                turnContext.Field,
                turnContext.Session,
                turnContext.StateTracker,
                turnContext.AutoHelp,
                turnContext.Escalation,
                turnContext.PrivacyReadbackPolicy,
                interpretation.InputTranscript,
                sttConfidencePercent,
                cancellationToken)
            .ConfigureAwait(false);

        if (spellingWorkflowResult.DialogResult is { } dialogResult)
        {
            // Ein während des Buchstabierens ausgeführter Befehl hat ein Dialogergebnis
            // geliefert. Dieses wird unverändert an den Formularablauf weitergereicht.
            return FormVoiceCommandFieldOrchestrationResult.Completed(FormVoiceCommandFieldHandlingResult.FromDialogResult(dialogResult));
        }

        // Nach Erkennerausfall oder Fokus- beziehungsweise Moduswechsel die
        // Feldaufforderung neu beginnen, ohne den bisherigen Puffer zu übernehmen.
        if (!spellingWorkflowResult.HasCapturedTranscript)
        {
            turnContext.StateTracker.BeginFieldPromptRestart();
            return FormVoiceCommandFieldOrchestrationResult.Completed(FormVoiceCommandFieldHandlingResult.Continue());
        }

        // Bereits zusammengesetzte Zeichen ohne erneute Buchstabierinterpretation
        // weitergeben. Die spätere Normalisierung und Validierung bleiben möglich.
        // Das Minimum der gemeldeten Vertrauenswerte der verbleibenden Segmente
        // übernehmen. Ohne Angaben bleibt es unbekannt, statt auf den Wert der
        // ersten Aufnahme zurückzugreifen.
        return FormVoiceCommandFieldOrchestrationResult.ContinueWithComposedValue(
            spellingWorkflowResult.CapturedTranscript,
            spellingWorkflowResult.AggregatedConfidencePercent);
    }
}
