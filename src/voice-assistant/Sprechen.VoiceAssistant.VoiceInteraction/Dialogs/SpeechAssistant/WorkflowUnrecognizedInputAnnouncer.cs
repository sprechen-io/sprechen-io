using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Protokolliert nicht zugeordnete Eingaben und Korrekturversuche beim
/// Buchstabieren, bei der Zifferneingabe und bei Mehrfachauswahl. Anschließend
/// wird der Fehleransagezustand gesetzt und die passende Rückmeldung angefordert.
/// </summary>
internal sealed class WorkflowUnrecognizedInputAnnouncer
{
    private readonly IEvaluationFormTelemetry _evaluationTelemetry;
    private readonly IFormVoiceCommandSpeechService _speechService;

    public WorkflowUnrecognizedInputAnnouncer(
        IEvaluationFormTelemetry evaluationTelemetry,
        IFormVoiceCommandSpeechService speechService)
    {
        _evaluationTelemetry = evaluationTelemetry ?? throw new ArgumentNullException(nameof(evaluationTelemetry));
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
    }

    public async Task AnnounceAsync(
        FormVoiceCommandContext context,
        FormFieldSnapshot field,
        EvaluationSession session,
        FormVoiceCommandStateTracker stateTracker,
        FormVoiceCommandUnrecognizedInputEscalationPolicy escalation,
        string rejectedTranscript,
        string repairEventCode,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(escalation);

        _evaluationTelemetry.TrackConstraintViolation(
            session,
            field.FieldId,
            FormVoiceCommandFieldErrorCodes.UnrecognizedInput,
            SeverityLevels.Warning,
            TelemetryErrorTriggers.SystemRecognition);
        _evaluationTelemetry.TrackRepairAttempt(session, field.FieldId, repairEventCode);
        stateTracker.BeginErrorPrompt();
        await _speechService.SpeakSafeAsync(
                // Keinen Wechsel in den bereits aktiven Eingabemodus anbieten.
                // Die Eingabe weiterhin als Feldwerteingabe behandeln, damit bei
                // geschlossenen Optionen die passenden Optionshinweise gewählt werden.
                escalation.SelectUnrecognizedInputPrompt(context, field, rejectedTranscript, FormRepairReason.UnknownSpellingToken, offerConstructiveFallback: false, isPrimaryFieldValueEntry: true, isAutoHelpDue: false),
                context.IsoCode,
                context.VoiceSettings,
                cancellationToken)
            .ConfigureAwait(false);
    }
}
