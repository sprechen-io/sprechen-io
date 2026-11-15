using Sprechen.Mda.Contracts.V1.Telemetry;
namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Behandelt globale Befehle sowie fehlgeschlagene Befehlszuordnungen. Je nach
/// Erkennungssicherheit wird ein Befehl ausgeführt, zur Bestätigung vorgelegt
/// oder zurückgewiesen.
/// </summary>
internal sealed class ParsedCommandBranch
{
    private readonly FormVoiceCommandGlobalCommandExecutionService _commandExecutionService;
    private readonly FormVoiceCommandFieldErrorHandlingService _fieldErrorHandlingService;
    private readonly FormVoiceCommandInputVsCommandTelemetry _ambiguityTelemetry;
    private readonly ParsedCommandBranchDecisionResolver _decisionResolver;
    private readonly FormVoiceCommandConfirmationDecisionService _confirmationDecisionService;
    private readonly IEvaluationCommandTelemetry _evaluationTelemetry;

    public ParsedCommandBranch(
        FormVoiceCommandGlobalCommandExecutionService commandExecutionService,
        FormVoiceCommandFieldErrorHandlingService fieldErrorHandlingService,
        FormVoiceCommandInputVsCommandTelemetry ambiguityTelemetry,
        FormVoiceCommandConfirmationDecisionService confirmationDecisionService,
        IEvaluationCommandTelemetry evaluationTelemetry)
        : this(
            commandExecutionService,
            fieldErrorHandlingService,
            ambiguityTelemetry,
            confirmationDecisionService,
            evaluationTelemetry,
            new ParsedCommandBranchDecisionResolver())
    {
    }

    public ParsedCommandBranch(
        FormVoiceCommandGlobalCommandExecutionService commandExecutionService,
        FormVoiceCommandFieldErrorHandlingService fieldErrorHandlingService,
        FormVoiceCommandInputVsCommandTelemetry ambiguityTelemetry,
        FormVoiceCommandConfirmationDecisionService confirmationDecisionService,
        IEvaluationCommandTelemetry evaluationTelemetry,
        ParsedCommandBranchDecisionResolver decisionResolver)
    {
        _commandExecutionService = commandExecutionService ?? throw new ArgumentNullException(nameof(commandExecutionService));
        _fieldErrorHandlingService = fieldErrorHandlingService ?? throw new ArgumentNullException(nameof(fieldErrorHandlingService));
        _ambiguityTelemetry = ambiguityTelemetry ?? throw new ArgumentNullException(nameof(ambiguityTelemetry));
        _confirmationDecisionService = confirmationDecisionService ?? throw new ArgumentNullException(nameof(confirmationDecisionService));
        _evaluationTelemetry = evaluationTelemetry ?? throw new ArgumentNullException(nameof(evaluationTelemetry));
        _decisionResolver = decisionResolver ?? throw new ArgumentNullException(nameof(decisionResolver));
    }

    public async Task<FormVoiceCommandFieldOrchestrationResult?> TryHandleAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        FormVoiceCommandTranscriptInterpretation interpretation,
        int? sttConfidencePercent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        ArgumentNullException.ThrowIfNull(interpretation);

        var decision = _decisionResolver.Resolve(turnContext.Context, interpretation);
        switch (decision.Kind)
        {
            case ParsedCommandBranchKind.None:
                return null;
            case ParsedCommandBranchKind.PromptedFailure:
                // Die fehlgeschlagene Befehlszuordnung als Erkennungsfehler protokollieren.
                _evaluationTelemetry.TrackCommandRecognitionFailure(
                    turnContext.Session,
                    decision.IssueCode,
                    commandId: null);
                return await HandlePromptedFailureAsync(
                    turnContext,
                    decision.IssueCode,
                    // Befehlsfehler nicht als Feldwerteingabe behandeln: weder einen anderen
                    // Eingabemodus noch die Optionsliste des aktuellen Felds anbieten.
                    turnContext.Escalation.SelectUnrecognizedInputPrompt(
                        turnContext.Context,
                        turnContext.Field,
                        interpretation.TrimmedTranscript,
                        FormRepairReason.UnparsedUtterance,
                        offerConstructiveFallback: false,
                        isPrimaryFieldValueEntry: false,
                        isAutoHelpDue: turnContext.AutoHelp.IsHelpDueOnNextFailure(turnContext.Field.FieldId)),
                    TelemetryErrorTriggers.SystemRecognition,
                    cancellationToken)
                .ConfigureAwait(false);
            case ParsedCommandBranchKind.ManualCorrection:
                var manualCorrectionResult = await _fieldErrorHandlingService.HandleManualCorrectionRequestedAsync(
                    turnContext,
                    cancellationToken)
                .ConfigureAwait(false);
                return FormVoiceCommandFieldOrchestrationResult.Completed(manualCorrectionResult);
            case ParsedCommandBranchKind.Execution:
                return await HandleExecutionDecisionAsync(turnContext, interpretation, sttConfidencePercent, cancellationToken).ConfigureAwait(false);
            default:
                throw new InvalidOperationException($"Unsupported parsed-command branch decision '{decision.Kind}'.");
        }
    }

    private async Task<FormVoiceCommandFieldOrchestrationResult> HandlePromptedFailureAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        string issueCode,
        string prompt,
        string trigger,
        CancellationToken cancellationToken)
    {
        var result = await _fieldErrorHandlingService.HandlePromptedFailureAsync(
                turnContext,
                issueCode,
                prompt,
                trigger,
                cancellationToken)
            .ConfigureAwait(false);
        return FormVoiceCommandFieldOrchestrationResult.Completed(result);
    }

    private async Task<FormVoiceCommandFieldOrchestrationResult> HandleExecutionDecisionAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        FormVoiceCommandTranscriptInterpretation interpretation,
        int? sttConfidencePercent,
        CancellationToken cancellationToken)
    {
        // Ohne gemeldete Konfidenz den Befehl ohne zusätzliche Konfidenzprüfung
        // ausführen. Gemeldete Werte bestimmen Ausführung, Rückfrage oder Ablehnung.
        if (!sttConfidencePercent.HasValue)
        {
            return await ExecuteCommandAsync(turnContext, interpretation, cancellationToken).ConfigureAwait(false);
        }

        var confidenceLevel = SttConfidenceGate.Evaluate(turnContext.Context, sttConfidencePercent);
        switch (confidenceLevel)
        {
            case SttConfidenceLevel.Sufficient:
                return await ExecuteCommandAsync(turnContext, interpretation, cancellationToken).ConfigureAwait(false);
            case SttConfidenceLevel.Uncertain:
                return await ConfirmThenExecuteCommandAsync(turnContext, interpretation, cancellationToken).ConfigureAwait(false);
            default:
                return await HandleLowConfidenceCommandAsync(turnContext, interpretation, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<FormVoiceCommandFieldOrchestrationResult> ConfirmThenExecuteCommandAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        FormVoiceCommandTranscriptInterpretation interpretation,
        CancellationToken cancellationToken)
    {
        var context = turnContext.Context;
        var confirmationPrompt = FormInputFeedbackPromptBuilder.GetAmbiguousInputVsCommandPrompt(
            context.Contract,
            context.IsoCode,
            interpretation.TrimmedTranscript,
            commandLabel: null,
            context.VoiceSettings.GetPolitenessLevel());

        var decision = await _confirmationDecisionService.CaptureDecisionAsync(
                context,
                turnContext.Session,
                turnContext.StateTracker,
                confirmationPrompt,
                cancellationToken)
            .ConfigureAwait(false);

        if (decision.Kind == ConfirmationDecisionKind.Continuation)
        {
            return await ExecuteCommandAsync(turnContext, interpretation, cancellationToken).ConfigureAwait(false);
        }

        if (decision.Kind == ConfirmationDecisionKind.Execution)
        {
            // Den während der Rückfrage erkannten Befehl direkt ausführen. Der ursprüngliche
            // Befehl wurde nicht bestätigt und darf daher keine Annahmemeldung erhalten.
            // Die offene Rückfrage wird anschließend nicht erneut gestellt.
            turnContext.AutoHelp.Reset();
            turnContext.Escalation.Reset();
            var interleavedResult = await _commandExecutionService.ExecuteAsync(
                    turnContext.Context,
                    turnContext.Interpreter,
                    turnContext.Session,
                    turnContext.StateTracker,
                    decision.CommandId,
                    decision.CommandArgument,
                    cancellationToken)
                .ConfigureAwait(false);
            return FormVoiceCommandFieldOrchestrationResult.Completed(interleavedResult);
        }

        return await HandleLowConfidenceCommandAsync(turnContext, interpretation, cancellationToken).ConfigureAwait(false);
    }

    private async Task<FormVoiceCommandFieldOrchestrationResult> HandleLowConfidenceCommandAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        FormVoiceCommandTranscriptInterpretation interpretation,
        CancellationToken cancellationToken)
    {
        // Die Ablehnung wegen niedriger Konfidenz als Erkennungsfehler protokollieren.
        _evaluationTelemetry.TrackCommandRecognitionFailure(
            turnContext.Session,
            FormVoiceCommandFieldErrorCodes.LowSttConfidence,
            commandId: null);
        return await HandlePromptedFailureAsync(
                turnContext,
                FormVoiceCommandFieldErrorCodes.LowSttConfidence,
                // Die Befehlsablehnung nicht als Feldwerteingabe behandeln. Deshalb weder
                // einen anderen Eingabemodus noch die Optionsliste des Felds anbieten.
                turnContext.Escalation.SelectUnrecognizedInputPrompt(
                    turnContext.Context,
                    turnContext.Field,
                    interpretation.TrimmedTranscript,
                    FormRepairReason.ConfidenceRejected,
                    offerConstructiveFallback: false,
                    isPrimaryFieldValueEntry: false,
                    isAutoHelpDue: turnContext.AutoHelp.IsHelpDueOnNextFailure(turnContext.Field.FieldId)),
                TelemetryErrorTriggers.SystemRecognition,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<FormVoiceCommandFieldOrchestrationResult> ExecuteCommandAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        FormVoiceCommandTranscriptInterpretation interpretation,
        CancellationToken cancellationToken)
    {
        _ambiguityTelemetry.TrackContextAccepted(
            turnContext.Session,
            turnContext.Field,
            interpretation);

        turnContext.AutoHelp.Reset();
        turnContext.Escalation.Reset();
        var commandResult = await _commandExecutionService.ExecuteAsync(
                turnContext.Context,
                turnContext.Interpreter,
                turnContext.Session,
                turnContext.StateTracker,
                interpretation.ParsedCommandId,
                interpretation.ParsedCommandArgument,
                cancellationToken)
            .ConfigureAwait(false);
        return FormVoiceCommandFieldOrchestrationResult.Completed(commandResult);
    }

}
