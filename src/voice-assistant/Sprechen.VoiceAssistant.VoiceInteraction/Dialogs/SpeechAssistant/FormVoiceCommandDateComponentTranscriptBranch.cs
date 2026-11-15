namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Leitet Eingabe mit gesetztem Ziffern- oder Buchstabierkennzeichen bei
/// effektivem Datumswerttyp an den Datumsbestandteildialog weiter. Andere
/// Feldtypen bleiben den nachfolgenden Eingabezweigen überlassen.
/// </summary>
internal sealed class FormVoiceCommandDateComponentTranscriptBranch
{
    private readonly DateComponentEntryWorkflowDriver _driver;

    public FormVoiceCommandDateComponentTranscriptBranch(DateComponentEntryWorkflowDriver driver)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
    }

    public async Task<FormVoiceCommandFieldOrchestrationResult?> TryHandleAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        FormVoiceCommandTranscriptInterpretation interpretation,
        int? sttConfidencePercent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        ArgumentNullException.ThrowIfNull(interpretation);

        if (!interpretation.IsDigitsMode && !interpretation.IsSpellingMode)
        {
            return null;
        }

        var field = turnContext.Field;
        var isDateField = string.Equals(
            FieldInteractionSemantics.ResolveEffectiveInteractionType(field.CodeStyle, field.ValueTypeKind),
            EffectiveFieldInteractionTypes.Date,
            StringComparison.Ordinal);
        if (!isDateField)
        {
            return null;
        }

        var result = await _driver.RunAsync(
                turnContext.Context,
                field,
                turnContext.Session,
                turnContext.StateTracker,
                turnContext.AutoHelp,
                interpretation.InputTranscript,
                sttConfidencePercent,
                cancellationToken)
            .ConfigureAwait(false);

        if (!result.HasCapturedValue)
        {
            // Ohne gesammelten Datumswert zur normalen Feldeingabe zurückkehren.
            turnContext.StateTracker.BeginFieldPromptRestart();
            return FormVoiceCommandFieldOrchestrationResult.Completed(FormVoiceCommandFieldHandlingResult.Continue());
        }

        // Das zusammengesetzte Datum mit der vom Datumsdialog gelieferten aggregierten
        // Erkennungssicherheit an die gemeinsame Wertanwendung geben. Keine fehlende
        // Angabe durch die Sicherheit der anfänglichen Äußerung ersetzen.
        return FormVoiceCommandFieldOrchestrationResult.ContinueWithTranscript(
            result.CapturedIsoDate!,
            ambiguityTelemetry: null,
            result.AggregatedConfidencePercent);
    }
}
