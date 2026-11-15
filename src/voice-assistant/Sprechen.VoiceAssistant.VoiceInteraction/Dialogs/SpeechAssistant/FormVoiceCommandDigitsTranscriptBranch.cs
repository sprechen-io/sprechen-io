namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Leitet erkannte Texte im Ziffernmodus an die Zifferneingabe weiter
/// und überführt deren Ergebnis in die weitere Feldverarbeitung.
/// </summary>
internal sealed class FormVoiceCommandDigitsTranscriptBranch
{
    private readonly FormVoiceCommandDigitsWorkflowService _digitsWorkflowService;

    public FormVoiceCommandDigitsTranscriptBranch(FormVoiceCommandDigitsWorkflowService digitsWorkflowService)
    {
        _digitsWorkflowService = digitsWorkflowService ?? throw new ArgumentNullException(nameof(digitsWorkflowService));
    }

    public async Task<FormVoiceCommandFieldOrchestrationResult?> TryHandleAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        FormVoiceCommandTranscriptInterpretation interpretation,
        int? sttConfidencePercent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        ArgumentNullException.ThrowIfNull(interpretation);

        if (!interpretation.IsDigitsMode)
        {
            return null;
        }

        var digitsWorkflowResult = await _digitsWorkflowService.CaptureDigitsTranscriptAsync(
                turnContext.Context,
                turnContext.Interpreter,
                turnContext.Field,
                turnContext.Session,
                turnContext.StateTracker,
                turnContext.AutoHelp,
                turnContext.Escalation,
                interpretation.InputTranscript,
                sttConfidencePercent,
                cancellationToken)
            .ConfigureAwait(false);

        if (digitsWorkflowResult.DialogResult is { } dialogResult)
        {
            // Ein während der Zifferneingabe ausgeführter Befehl hat ein Dialogergebnis
            // geliefert. Dieses wird unverändert an den Formularablauf weitergereicht.
            return FormVoiceCommandFieldOrchestrationResult.Completed(FormVoiceCommandFieldHandlingResult.FromDialogResult(dialogResult));
        }

        // Nach Erkennerausfall oder Fokus- beziehungsweise Moduswechsel die
        // Feldaufforderung neu beginnen. Der bisherige Puffer wird nicht übernommen.
        if (!digitsWorkflowResult.HasCapturedTranscript)
        {
            turnContext.StateTracker.BeginFieldPromptRestart();
            return FormVoiceCommandFieldOrchestrationResult.Completed(FormVoiceCommandFieldHandlingResult.Continue());
        }

        // Den kleinsten gemeldeten Vertrauenswert der verbleibenden Ziffernblöcke
        // weitergeben. Fehlen alle Angaben, bleibt der Wert unbekannt. Nicht durch
        // die Konfidenz der ersten Aufnahme ersetzen: Deren Inhalt könnte verworfen
        // oder später neu diktiert worden sein.
        return FormVoiceCommandFieldOrchestrationResult.ContinueWithTranscript(
            digitsWorkflowResult.CapturedTranscript,
            ambiguityTelemetry: null,
            digitsWorkflowResult.AggregatedConfidencePercent);
    }
}
