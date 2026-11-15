using Sprechen.Mda.Contracts.V1.Telemetry;


namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Fordert die Navigation zum nächsten Validierungsverstoß an und ordnet dessen Telemetrie und Rückmeldung als Sichtbarmachung eines bestehenden Befunds ein.
/// </summary>
internal sealed class FormVoiceCommandErrorHandler : IFormVoiceCommandGlobalCommandHandler
{
    private readonly IEvaluationFormTelemetry _evaluationTelemetry;
    private readonly IFormVoiceCommandSpeechService _speechService;

    public FormVoiceCommandErrorHandler(
        IEvaluationFormTelemetry evaluationTelemetry,
        IFormVoiceCommandSpeechService speechService)
    {
        _evaluationTelemetry = evaluationTelemetry ?? throw new ArgumentNullException(nameof(evaluationTelemetry));
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
    }

    public bool CanHandle(CommandId commandId) => commandId == GlobalCommandIds.NextError;

    public async Task<VoiceCommandDialogResult?> HandleAsync(
        FormVoiceCommandGlobalCommandContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var politenessLevel = context.VoiceSettings.GetPolitenessLevel();

        var violation = await context.Interpreter.TryFocusNextErrorAsync().ConfigureAwait(false);
        if (violation is null)
        {
            _evaluationTelemetry.TrackNavigationAidNoTarget(context.Session, GlobalCommandIds.NextError);
            await _speechService.SpeakSafeAsync(
                    FormSummaryPromptBuilder.GetNoErrorsPrompt(context.Contract, context.IsoCode, politenessLevel),
                    context.IsoCode,
                    context.VoiceSettings,
                    cancellationToken)
                .ConfigureAwait(false);
            return null;
        }

        // Die Navigation meldet einen vorhandenen Validierungsverstoß.
        // Diesen nicht als neu fehlgeschlagene Werteingabe kennzeichnen.
        _evaluationTelemetry.TrackConstraintViolation(
            context.Session,
            violation.FieldId,
            violation.Code,
            violation.Severity,
            violation.Scope,
            violation.FieldGroupId,
            TelemetryErrorTriggers.RuleVisibility);
        _evaluationTelemetry.TrackRepairAttempt(context.Session, violation.FieldId, violation.ConstraintId);

        var focusedField = await context.Interpreter.GetFocusedFieldAsync().ConfigureAwait(false);

        context.StateTracker.BeginErrorPrompt();
        await _speechService.SpeakSafeAsync(
                FormConstraintViolationPromptFormatter.FormatViolationPrompt(
                    context.Contract,
                    context.IsoCode,
                    politenessLevel,
                    violation,
                    focusedField?.Label,
                    understoodValue: null,
                    includesValue: false),
                context.IsoCode,
                context.VoiceSettings,
                cancellationToken)
            .ConfigureAwait(false);

        return null;
    }
}
