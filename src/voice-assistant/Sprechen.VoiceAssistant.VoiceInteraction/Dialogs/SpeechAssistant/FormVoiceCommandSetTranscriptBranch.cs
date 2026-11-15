using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Leitet Felder mit Mehrfachauswahl an einen Ablauf zum schrittweisen
/// Sammeln von Optionen weiter. Er gilt bei unbegrenzter Auswahl oder
/// einem Maximum größer als eins. Auf eine Option begrenzte Felder
/// bleiben bei der gewöhnlichen Eingabeverarbeitung.
/// </summary>
internal sealed class FormVoiceCommandSetTranscriptBranch
{
    private readonly FormVoiceCommandSetWorkflowService _setWorkflowService;

    public FormVoiceCommandSetTranscriptBranch(FormVoiceCommandSetWorkflowService setWorkflowService)
    {
        _setWorkflowService = setWorkflowService ?? throw new ArgumentNullException(nameof(setWorkflowService));
    }

    public async Task<FormVoiceCommandFieldOrchestrationResult?> TryHandleAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        FormVoiceCommandTranscriptInterpretation interpretation,
        int? sttConfidencePercent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        ArgumentNullException.ThrowIfNull(interpretation);

        if (!QualifiesForSetAccumulation(turnContext.Field))
        {
            return null;
        }

        var setWorkflowResult = await _setWorkflowService.CaptureSetTranscriptAsync(
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

        if (setWorkflowResult.DialogResult is { } dialogResult)
        {
            // Ein während der Auswahl ausgeführter Befehl hat ein Dialogergebnis
            // geliefert. Dieses wird unverändert an den Formularablauf weitergereicht.
            return FormVoiceCommandFieldOrchestrationResult.Completed(FormVoiceCommandFieldHandlingResult.FromDialogResult(dialogResult));
        }

        // Nach Erkennerausfall oder Fokus- beziehungsweise Moduswechsel die
        // Feldaufforderung neu beginnen, ohne den bisherigen Puffer zu übernehmen.
        if (!setWorkflowResult.HasCapturedTranscript)
        {
            turnContext.StateTracker.BeginFieldPromptRestart();
            return FormVoiceCommandFieldOrchestrationResult.Completed(FormVoiceCommandFieldHandlingResult.Continue());
        }

        // Eine abgeschlossene leere Auswahl über das dafür vorgesehene Ergebnis
        // weitergeben. Der gewöhnliche Transkriptweg verlangt nichtleeren Text.
        // Den vom Eingabeablauf gelieferten Vertrauenswert unverändert übernehmen,
        // auch wenn er unbekannt bleibt.
        if (setWorkflowResult.CapturedTranscript.Length == 0)
        {
            return FormVoiceCommandFieldOrchestrationResult.ContinueWithEmptySelection(
                setWorkflowResult.AggregatedConfidencePercent);
        }

        // Bei nichtleerer Auswahl das Kennzeichen einer ausdrücklichen Übernahme
        // weitergeben. Die folgende Bestätigung kann dadurch eine zusätzliche
        // Frage auslassen. Ein Abschluss nach fehlender Sprache setzt dieses
        // Kennzeichen nicht.
        return FormVoiceCommandFieldOrchestrationResult.ContinueWithTranscript(
            setWorkflowResult.CapturedTranscript,
            ambiguityTelemetry: null,
            setWorkflowResult.AggregatedConfidencePercent,
            finalizedByExplicitAccept: setWorkflowResult.FinalizedByExplicitAccept);
    }

    // Mehrfachauswahl schrittweise sammeln, wenn kein Maximum oder ein
    // Maximum größer als eins vorgegeben ist. Bei höchstens einer Option
    // bleibt die gewöhnliche Eingabeverarbeitung zuständig.
    private static bool QualifiesForSetAccumulation(FormFieldSnapshot field)
        => string.Equals(
                FieldInteractionSemantics.ResolveEffectiveInteractionType(field.CodeStyle, field.ValueTypeKind),
                EffectiveFieldInteractionTypes.MultiSelect,
                StringComparison.Ordinal)
            && field.MaxSelected is null or > 1;
}
