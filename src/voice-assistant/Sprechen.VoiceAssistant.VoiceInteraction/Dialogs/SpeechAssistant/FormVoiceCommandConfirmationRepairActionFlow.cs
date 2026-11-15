using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Verarbeitet unklare Bestätigungsantworten, gewünschte Wertkorrekturen,
/// den Wechsel zum Buchstabieren und globale Befehle während der Feldbestätigung.
/// </summary>
internal sealed class FormVoiceCommandConfirmationRepairActionFlow
{
    private readonly IEvaluationFormTelemetry _evaluationTelemetry;
    private readonly IFormVoiceCommandSpeechService _speechService;
    private readonly FormVoiceCommandGlobalCommandExecutionService _commandExecutionService;

    public FormVoiceCommandConfirmationRepairActionFlow(
        IEvaluationFormTelemetry evaluationTelemetry,
        IFormVoiceCommandSpeechService speechService,
        FormVoiceCommandGlobalCommandExecutionService commandExecutionService)
    {
        _evaluationTelemetry = evaluationTelemetry ?? throw new ArgumentNullException(nameof(evaluationTelemetry));
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
        _commandExecutionService = commandExecutionService ?? throw new ArgumentNullException(nameof(commandExecutionService));
    }

    public async Task<FormVoiceCommandFieldHandlingResult?> TryHandleAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        ConfirmationDecision decision,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(turnContext);

        return decision.Kind switch
        {
            ConfirmationDecisionKind.Undecided
                => await HandleUndecidedAsync(turnContext, cancellationToken).ConfigureAwait(false),
            ConfirmationDecisionKind.CurrentValueEdit
                => await HandleEditAsync(turnContext, cancellationToken).ConfigureAwait(false),
            ConfirmationDecisionKind.SpellingSwitch
                => await HandleSwitchToSpellingAsync(turnContext, cancellationToken).ConfigureAwait(false),
            ConfirmationDecisionKind.Execution
                => await HandleExecutionAsync(turnContext, decision, cancellationToken).ConfigureAwait(false),
            _ => null
        };
    }

    // Globale Befehle über dieselbe Ausführungsfunktion wie im übrigen
    // Formulardialog weiterleiten. Das gilt auch, wenn die Bestätigungsfrage
    // nach unsicherer Buchstabenerkennung gestellt wurde.
    private async Task<FormVoiceCommandFieldHandlingResult> HandleExecutionAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        ConfirmationDecision decision,
        CancellationToken cancellationToken)
    {
        turnContext.AutoHelp.Reset();
        turnContext.Escalation.Reset();
        return await _commandExecutionService.ExecuteAsync(
                turnContext.Context,
                turnContext.Interpreter,
                turnContext.Session,
                turnContext.StateTracker,
                decision.CommandId,
                decision.CommandArgument,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<FormVoiceCommandFieldHandlingResult> HandleUndecidedAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        CancellationToken cancellationToken)
    {
        var context = turnContext.Context;
        _evaluationTelemetry.TrackRepairAttempt(
            turnContext.Session,
            turnContext.Field.FieldId,
            FormVoiceCommandFieldErrorCodes.UnrecognizedInput);
        turnContext.StateTracker.BeginFieldPromptRestart();
        await _speechService.SpeakSafeAsync(
                // Eine unklare Bestätigungsantwort ist keine neue Feldwerteingabe.
                // Hier deshalb weder einen Eingabemodus noch die Optionsliste des Felds
                // anbieten. Die Rückmeldung soll sich auf die Bestätigung beziehen.
                turnContext.Escalation.SelectUnrecognizedInputPrompt(
                    context,
                    turnContext.Field,
                    recognizedTranscript: null,
                    FormRepairReason.NothingCaptured,
                    offerConstructiveFallback: false,
                    isPrimaryFieldValueEntry: false,
                    isAutoHelpDue: false),
                context.IsoCode,
                context.VoiceSettings,
                cancellationToken)
            .ConfigureAwait(false);

        return FormVoiceCommandFieldHandlingResult.Continue();
    }

    private async Task<FormVoiceCommandFieldHandlingResult> HandleEditAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        CancellationToken cancellationToken)
    {
        var context = turnContext.Context;
        TrackRepair(turnContext, FormVoiceCommandFieldErrorCodes.ManualCorrection);
        await SpeakCorrectionRequestedAsync(context, cancellationToken).ConfigureAwait(false);
        return FormVoiceCommandFieldHandlingResult.Continue();
    }

    private async Task<FormVoiceCommandFieldHandlingResult> HandleSwitchToSpellingAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        CancellationToken cancellationToken)
    {
        var context = turnContext.Context;
        TrackRepair(turnContext, FormVoiceCommandFieldErrorCodes.SwitchToSpelling);
        if (await FormVoiceCommandInputModeChangeTracker.TryApplyAndTrackAsync(
                turnContext.Interpreter,
                _evaluationTelemetry,
                turnContext.Session,
                InputMode.Spelling,
                TelemetryInputModeTriggers.ExplicitCommand).ConfigureAwait(false))
        {
            await _speechService.SpeakSafeAsync(
                    FormInputFeedbackPromptBuilder.GetInputModeChangedPrompt(
                        context.Contract,
                        context.IsoCode,
                        InputMode.Spelling,
                        context.VoiceSettings.GetPolitenessLevel()),
                    context.IsoCode,
                    context.VoiceSettings,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        await SpeakCorrectionRequestedAsync(context, cancellationToken).ConfigureAwait(false);
        return FormVoiceCommandFieldHandlingResult.Continue();
    }

    /// <summary>
    /// Protokolliert die Rückfrage mit dem Grund fehlende oder nicht zuordenbare
    /// Eingabe. Anders als <see cref="TrackRepair"/> wird dabei kein Wechsel
    /// in die Feldkorrektur ausgelöst. Die Bestätigung bleibt aktiv.
    /// </summary>
    public void TrackReask(FormVoiceCommandFieldTurnContext turnContext, ConfirmationUndecidedReason reason)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        _evaluationTelemetry.TrackRepairAttempt(
            turnContext.Session,
            turnContext.Field.FieldId,
            reason == ConfirmationUndecidedReason.Silence
                ? FormVoiceCommandFieldErrorCodes.NoInput
                : FormVoiceCommandFieldErrorCodes.UnrecognizedInput);
    }

    private void TrackRepair(FormVoiceCommandFieldTurnContext turnContext, string errorCode)
    {
        _evaluationTelemetry.TrackRepairAttempt(turnContext.Session, turnContext.Field.FieldId, errorCode);
        turnContext.StateTracker.BeginFieldRepair();
    }

    private Task SpeakCorrectionRequestedAsync(
        FormVoiceCommandContext context,
        CancellationToken cancellationToken)
    {
        return _speechService.SpeakSafeAsync(
            FormInputFeedbackPromptBuilder.GetCorrectionRequestedPrompt(
                context.Contract,
                context.IsoCode,
                context.VoiceSettings.GetPolitenessLevel()),
            context.IsoCode,
            context.VoiceSettings,
            cancellationToken);
    }
}
