using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft den Formularabschluss über Regeln und Validierung. Bei Verstößen fordert der Dienst Reparaturhinweise an. Ein erfolgreicher Rückgabewert speichert das Ergebnis nicht selbst.
/// </summary>
internal sealed class FormVoiceCommandSubmissionService : IFormVoiceCommandSubmissionService
{
    private readonly IEvaluationFormTelemetry _evaluationTelemetry;
    private readonly IFormVoiceCommandSpeechService _speechService;

    public FormVoiceCommandSubmissionService(
        IEvaluationFormTelemetry evaluationTelemetry,
        IFormVoiceCommandSpeechService speechService)
    {
        _evaluationTelemetry = evaluationTelemetry ?? throw new ArgumentNullException(nameof(evaluationTelemetry));
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
    }

    public async Task<FormSubmissionResult> TrySubmitAsync(
        IFormDialogRuntime interpreter,
        FormContract contract,
        EvaluationSession session,
        string isoCode,
        VoiceDialogSettings voiceSettings,
        IFormVoiceCommandDialogStateTracker stateTracker,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(interpreter);
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);
        ArgumentNullException.ThrowIfNull(voiceSettings);
        ArgumentNullException.ThrowIfNull(stateTracker);

        _ = await interpreter.TryApplySubmitAttemptRulesAsync().ConfigureAwait(false);

        var violations = await interpreter.ValidateAllAsync().ConfigureAwait(false);
        if (violations.Length == 0)
        {
            return FormSubmissionResult.Submitted;
        }

        var nextError = await interpreter.TryFocusNextErrorAsync().ConfigureAwait(false);
        var spokenViolation = nextError ?? (violations.Length > 0 ? violations[0] : null);
        if (spokenViolation is null)
        {
            return FormSubmissionResult.BlockedUnresolvable;
        }

        // Die für den Formularversuch ausgewählten offenen Verstöße protokollieren.
        // Der einzelne Verstoß für die spätere Ansage wurde bereits getrennt ausgewählt.
        foreach (var openError in FormOpenErrorResolver.ResolveOpenErrors(violations))
        {
            _evaluationTelemetry.TrackConstraintViolation(
                session,
                ResolveTelemetryFieldId(openError),
                openError.Code,
                openError.Severity,
                openError.Scope,
                openError.FieldGroupId,
                TelemetryErrorTriggers.SubmitAttempt);
        }

        // Den Korrekturversuch dem für die Rückmeldung ausgewählten Verstoß zuordnen.
        _evaluationTelemetry.TrackRepairAttempt(
            session,
            ResolveTelemetryFieldId(spokenViolation),
            spokenViolation.ConstraintId);

        var focusedField = await interpreter.GetFocusedFieldAsync().ConfigureAwait(false);

        stateTracker.BeginErrorPrompt();
        await _speechService.SpeakSafeAsync(
                FormConstraintViolationPromptFormatter.FormatViolationPrompt(
                    contract,
                    isoCode,
                    voiceSettings.GetPolitenessLevel(),
                    spokenViolation,
                    focusedField?.Label,
                    understoodValue: null,
                    includesValue: false),
                isoCode,
                voiceSettings,
                cancellationToken)
            .ConfigureAwait(false);
        return nextError is null
            ? FormSubmissionResult.BlockedUnresolvable
            : FormSubmissionResult.BlockedAwaitingCorrection;
    }

    private static string ResolveTelemetryFieldId(FormConstraintViolation violation)
        => string.IsNullOrWhiteSpace(violation.FieldId)
            ? violation.ConstraintId
            : violation.FieldId;
}
