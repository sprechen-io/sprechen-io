using Sprechen.Mda.Contracts.V1.Telemetry;
using Sprechen.Speech.App.Contracts.Services;


namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Verarbeitet ein Transkript oder einen bereits zusammengesetzten Wert für
/// das fokussierte Feld. Prüft Erkennungsvertrauen, übernimmt und validiert
/// den Wert und bietet gegebenenfalls Korrekturen oder weitere Feldwerte an.
/// Anschließend folgt die Feldbestätigung.
/// </summary>
internal sealed class FormVoiceCommandFieldApplyService
{
    private readonly IEvaluationFormTelemetry _formTelemetry;
    private readonly IEvaluationCommandTelemetry _commandTelemetry;
    private readonly FormVoiceCommandFieldErrorHandlingService _fieldErrorHandlingService;
    private readonly FormVoiceCommandConfirmationHandler _confirmationHandler;
    private readonly FormVoiceCommandConfirmationValuePresenter _confirmationValuePresenter;
    private readonly FormVoiceCommandAdditionalFieldProposalHandler _additionalFieldProposalHandler;
    private readonly FormVoiceCommandSpellingEscalationFlow _spellingEscalationFlow;
    private readonly FormRepairReasonResolver _repairReasonResolver;

    // Ohne konfigurierten Diagnoseempfänger wird NullDialogDecisionSink
    // verwendet. Die Entscheidungspunkte benötigen dadurch keine Nullprüfung.
    private readonly IDialogDecisionSink _decisionSink;

    public FormVoiceCommandFieldApplyService(
        IEvaluationFormTelemetry formTelemetry,
        IEvaluationCommandTelemetry commandTelemetry,
        FormVoiceCommandFieldErrorHandlingService fieldErrorHandlingService,
        FormVoiceCommandConfirmationHandler confirmationHandler,
        FormVoiceCommandConfirmationValuePresenter confirmationValuePresenter,
        FormVoiceCommandAdditionalFieldProposalHandler additionalFieldProposalHandler,
        FormVoiceCommandSpellingEscalationFlow spellingEscalationFlow,
        FormRepairReasonResolver repairReasonResolver,
        IDialogDecisionSink? decisionSink = null)
    {
        _formTelemetry = formTelemetry ?? throw new ArgumentNullException(nameof(formTelemetry));
        _commandTelemetry = commandTelemetry ?? throw new ArgumentNullException(nameof(commandTelemetry));
        _fieldErrorHandlingService = fieldErrorHandlingService ?? throw new ArgumentNullException(nameof(fieldErrorHandlingService));
        _confirmationHandler = confirmationHandler ?? throw new ArgumentNullException(nameof(confirmationHandler));
        _confirmationValuePresenter = confirmationValuePresenter ?? throw new ArgumentNullException(nameof(confirmationValuePresenter));
        _additionalFieldProposalHandler = additionalFieldProposalHandler ?? throw new ArgumentNullException(nameof(additionalFieldProposalHandler));
        _spellingEscalationFlow = spellingEscalationFlow ?? throw new ArgumentNullException(nameof(spellingEscalationFlow));
        _repairReasonResolver = repairReasonResolver ?? throw new ArgumentNullException(nameof(repairReasonResolver));
        _decisionSink = decisionSink ?? NullDialogDecisionSink.Instance;
    }

    public async Task<FormVoiceCommandFieldHandlingResult> ApplyAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        FormVoiceCommandFieldApplyRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        ArgumentNullException.ThrowIfNull(request);

        var context = turnContext.Context;
        var interpreter = turnContext.Interpreter;
        var field = turnContext.Field;
        var session = turnContext.Session;
        var stateTracker = turnContext.StateTracker;
        var autoHelp = turnContext.AutoHelp;

        // Nur für dafür freigegebenes Freitextdiktat mit vorgesehener Wertwiedergabe
        // die untere Vertrauensgrenze lockern. Buchstabieren, Code- und andere
        // eingeschränkte Typen sowie maskierte Werte behalten die strengere Prüfung.
        var relaxBelowConfirmFloor = FreeTextConfidenceRelaxationPolicy.AllowsAcceptWithReadback(
            request.ExpectedInputMode,
            field.CodeStyle,
            field.ValueTypeKind,
            field.SpeakValueInConfirmation);
        var confidence = SttConfidenceGate.Assess(context, request.SttConfidencePercent, relaxBelowConfirmFloor);
        var confidenceLevel = confidence.Level;
        await _decisionSink
            .RecordAsync(
                DialogDecisions.Confidence(
                    request.SttConfidencePercent,
                    confidence.AppliedConfirmThresholdPercent,
                    confidenceLevel.ToString()),
                cancellationToken)
            .ConfigureAwait(false);

        if (confidenceLevel == SttConfidenceLevel.Insufficient)
        {
            _formTelemetry.TrackFieldAttempt(
                session,
                field.FieldId,
                request.ExpectedInputMode.ToWireValue(),
                success: false,
                sttConfidencePercent: request.SttConfidencePercent);

            // Bei zu niedrigem Vertrauen nach Buchstabieren gezielte Korrekturen
            // anbieten. Wird dieser Fall nicht gesondert behandelt, folgt die
            // allgemeine Rückmeldung zur abgelehnten Erkennung.
            var spellingEscalationResult = await _spellingEscalationFlow
                .TryEscalateAsync(turnContext, request, confidenceLevel, cancellationToken)
                .ConfigureAwait(false);
            if (spellingEscalationResult is not null)
            {
                return spellingEscalationResult;
            }

            // Die Anwendung lehnt die Erkennung wegen zu niedrigem Vertrauen ab.
            // Das Transkript soll deshalb nicht als verlässliche Wiedergabe der
            // tatsächlich gesprochenen Antwort verwendet werden.
            return await _fieldErrorHandlingService.HandlePromptedFailureAsync(
                    turnContext,
                    FormVoiceCommandFieldErrorCodes.LowSttConfidence,
                    turnContext.Escalation.SelectUnrecognizedInputPrompt(
                        context,
                        field,
                        request.TranscriptForApply,
                        FormRepairReason.ConfidenceRejected,
                        offerConstructiveFallback: ShouldOfferConstructiveFallback(field),
                        isPrimaryFieldValueEntry: true,
                        isAutoHelpDue: autoHelp.IsHelpDueOnNextFailure(field.FieldId)),
                    TelemetryErrorTriggers.SystemRecognition,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        // Bereits zusammengesetzte Werte ohne erneute Buchstaben- oder
        // Zifferninterpretation anwenden. Die weitere Normalisierung und
        // Validierung bleiben möglich.
        var inputResult = request.IsComposedValue
            ? await interpreter.TryApplyComposedValueAsync(request.TranscriptForApply).ConfigureAwait(false)
            : await interpreter.TryApplyTranscriptAsync(request.TranscriptForApply).ConfigureAwait(false);
        if (request.IsComposedValue)
        {
            await _decisionSink
                .RecordAsync(
                    DialogDecisions.ValueType(
                        field.ValueTypeKind,
                        request.TranscriptForApply.Length,
                        inputResult.ValueCaptured,
                        inputResult.NotCapturedReason.ToString()),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        _formTelemetry.TrackFieldAttempt(
            session,
            field.FieldId,
            request.ExpectedInputMode.ToWireValue(),
            success: inputResult.ValueCaptured,
            sttConfidencePercent: request.SttConfidencePercent);

        if (request.AmbiguityTelemetry is not null)
        {
            var resolvedAmbiguity = request.AmbiguityTelemetry.Value;
            _commandTelemetry.TrackAmbiguityResolved(
                session,
                resolvedAmbiguity.AmbiguityType,
                resolvedAmbiguity.ResolutionStrategy,
                resolvedAmbiguity.CommandSignalClass,
                resolvedAmbiguity.Result,
                field.FieldId,
                resolvedAmbiguity.CommandId);
        }

        if (!inputResult.ValueCaptured)
        {
            // Den gemeldeten Grund der fehlgeschlagenen Wertübernahme zur Wahl
            // der Korrekturrückmeldung verwenden.
            var repairReason = _repairReasonResolver.Resolve(
                inputResult.ParseFailure,
                confidenceRejected: false,
                request.TranscriptForApply,
                context.IsoCode);

            return await _fieldErrorHandlingService.HandlePromptedFailureAsync(
                    turnContext,
                    FormVoiceCommandFieldErrorCodes.UnrecognizedInput,
                    turnContext.Escalation.SelectUnrecognizedInputPrompt(
                        context,
                        field,
                        request.TranscriptForApply,
                        repairReason,
                        offerConstructiveFallback: ShouldOfferConstructiveFallback(field),
                        isPrimaryFieldValueEntry: true,
                        isAutoHelpDue: autoHelp.IsHelpDueOnNextFailure(field.FieldId)),
                    TelemetryErrorTriggers.SystemRecognition,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        autoHelp.Reset();
        turnContext.Escalation.Reset();
        stateTracker.BeginValidation();

        if (inputResult.Violations.Length > 0)
        {
            foreach (var violation in inputResult.Violations)
            {
                _formTelemetry.TrackConstraintViolation(
                    session,
                    violation.FieldId,
                    violation.Code,
                    violation.Severity,
                    violation.Scope,
                    violation.FieldGroupId,
                    TelemetryErrorTriggers.UserInput);
            }

            var firstViolation = inputResult.Violations[0];
            var understoodValue = await _confirmationValuePresenter
                .BuildUnderstoodValueAsync(turnContext, request.TranscriptForApply)
                .ConfigureAwait(false);

            return await _fieldErrorHandlingService.HandleValidationViolationAsync(
                    turnContext,
                    firstViolation.ConstraintId,
                    FormConstraintViolationPromptFormatter.FormatViolationPrompt(
                        context.Contract,
                        context.IsoCode,
                        context.VoiceSettings.GetPolitenessLevel(),
                        firstViolation,
                        field.Label,
                        understoodValue.Value,
                        understoodValue.IncludesValue),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        // Nach erfolgreicher Übernahme ohne Validierungsverstoß zusätzlich erkannte
        // Feldwerte einzeln zur ausdrücklichen Zustimmung anbieten. Ohne weitere
        // Kandidaten endet dieser Schritt ohne Angebot.
        await _additionalFieldProposalHandler.ProposeAsync(
                turnContext,
                request.TranscriptForApply,
                cancellationToken)
            .ConfigureAwait(false);

        return await _confirmationHandler.HandleAsync(
                turnContext,
                new FormVoiceCommandConfirmationRequest(
                    request.TranscriptForApply,
                    request.FinalizedByExplicitAccept,
                    fromSpellingOrDigits: request.ExpectedInputMode is InputMode.Spelling or InputMode.Digits),
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static bool ShouldOfferConstructiveFallback(FormFieldSnapshot field)
    {
        var effectiveInteractionType = FieldInteractionSemantics.ResolveEffectiveInteractionType(
            field.CodeStyle,
            field.ValueTypeKind);

        return effectiveInteractionType is not EffectiveFieldInteractionTypes.Date
            and not EffectiveFieldInteractionTypes.Time
            and not EffectiveFieldInteractionTypes.MultiSelect;
    }
}
