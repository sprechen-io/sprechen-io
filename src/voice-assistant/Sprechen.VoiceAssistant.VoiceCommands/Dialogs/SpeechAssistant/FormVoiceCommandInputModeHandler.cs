using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.VoiceAssistant.VoiceCommands.Dialogs.SpeechAssistant;

/// <summary>
/// Bearbeitet explizite Eingabemodusbefehle mit Zulassungsprüfung und
/// hörbarer Rückmeldung. Tastatureingaben werden als eigener Dialogauftrag zurückgegeben.
/// </summary>
internal sealed class FormVoiceCommandInputModeHandler : IFormVoiceCommandGlobalCommandHandler
{
    private readonly IEvaluationFormTelemetry _evaluationTelemetry;
    private readonly IFormVoiceCommandSpeechService _speechService;

    public FormVoiceCommandInputModeHandler(
        IEvaluationFormTelemetry evaluationTelemetry,
        IFormVoiceCommandSpeechService speechService)
    {
        _evaluationTelemetry = evaluationTelemetry ?? throw new ArgumentNullException(nameof(evaluationTelemetry));
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
    }

    public bool CanHandle(CommandId commandId)
        => commandId.Value is GlobalCommandIds.InputModeSpelling
            or GlobalCommandIds.InputModeVoice
            or GlobalCommandIds.InputModeDigits
            or GlobalCommandIds.InputModeKeyboard
            or GlobalCommandIds.InputModeLiteral
            or GlobalCommandIds.InputModeLiteralOff;

    public async Task<VoiceCommandDialogResult?> HandleAsync(
        FormVoiceCommandGlobalCommandContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        var politenessLevel = context.VoiceSettings.GetPolitenessLevel();

        var targetInputMode = context.CommandId switch
        {
            GlobalCommandIds.InputModeSpelling => InputMode.Spelling,
            GlobalCommandIds.InputModeDigits => InputMode.Digits,
            GlobalCommandIds.InputModeKeyboard => InputMode.Keyboard,
            GlobalCommandIds.InputModeLiteral => InputMode.Literal,
            GlobalCommandIds.InputModeLiteralOff => InputMode.Voice,
            _ => InputMode.Voice
        };

        if (targetInputMode == InputMode.Keyboard)
        {
            return await RequestSilentInputAsync(context, politenessLevel, cancellationToken)
                .ConfigureAwait(false);
        }

        if (!await FormVoiceCommandInputModeChangeTracker.TryApplyAndTrackAsync(
                context.Interpreter,
                _evaluationTelemetry,
                context.Session,
                targetInputMode,
                TelemetryInputModeTriggers.ExplicitCommand).ConfigureAwait(false))
        {
            // Der Moduswechsel wurde abgelehnt. Keine Aktivierung bestätigen,
            // sondern die vorgesehene Rückmeldung zur fehlenden Verfügbarkeit sprechen.
            await SpeakInputModeDeniedAsync(
                    context,
                    targetInputMode,
                    politenessLevel,
                    cancellationToken)
                .ConfigureAwait(false);
            return null;
        }

        // Beim ersten Eintritt ins Buchstabieren die ausführliche Erklärung wählen,
        // später die kurze Erinnerung. Die Abfrage merkt sich das Thema bereits vor der Ansage.
        var teachFullSpellingTail = targetInputMode != InputMode.Spelling
            || !context.StateTracker.HasAlreadyTaught(DialogTeachOnceTopics.SpellingModeTail);

        await _speechService.SpeakSafeAsync(
                FormInputFeedbackPromptBuilder.GetInputModeChangedPrompt(
                    context.Contract,
                    context.IsoCode,
                    targetInputMode,
                    politenessLevel,
                    teachFullSpellingTail),
                context.IsoCode,
                context.VoiceSettings,
                cancellationToken)
            .ConfigureAwait(false);

        return null;
    }

    private async Task<VoiceCommandDialogResult?> RequestSilentInputAsync(
        FormVoiceCommandGlobalCommandContext context,
        PolitenessLevel politenessLevel,
        CancellationToken cancellationToken)
    {
        var eligibility = await context.Interpreter
            .SilentInput
            .GetFocusedSilentInputEligibilityAsync()
            .ConfigureAwait(false);
        if (eligibility is null)
        {
            await _speechService.SpeakSafeAsync(
                    FormInputFeedbackPromptBuilder.GetSilentInputUnavailablePrompt(
                        context.Contract,
                        context.IsoCode,
                        politenessLevel),
                    context.IsoCode,
                    context.VoiceSettings,
                    cancellationToken)
                .ConfigureAwait(false);
            return null;
        }

        return VoiceCommandDialogResult.SilentInputRequested(eligibility);
    }

    private async Task SpeakInputModeDeniedAsync(
        FormVoiceCommandGlobalCommandContext context,
        InputMode targetInputMode,
        PolitenessLevel politenessLevel,
        CancellationToken cancellationToken)
    {
        var availability = await context.Interpreter
            .GetFocusedFieldInputModeAvailabilityAsync(targetInputMode.ToWireValue())
            .ConfigureAwait(false);

        var focusedField = await context.Interpreter.GetFocusedFieldAsync().ConfigureAwait(false);
        _evaluationTelemetry.TrackConstraintViolation(
            context.Session,
            focusedField.FieldId,
            InputModeAdmissionErrorCodes.InputModeUnavailable,
            SeverityLevels.Warning,
            TelemetryErrorTriggers.UserInput);

        var prompt = availability.UnavailabilityReason == FormFieldInputModeUnavailabilityReason.VoiceInputForbidden
            ? FormInputFeedbackPromptBuilder.GetVoiceInputForbiddenPrompt(
                context.Contract,
                context.IsoCode,
                politenessLevel,
                await ResolveSilentInputFallbackLabelAsync(context, focusedField.VariableId, politenessLevel, cancellationToken)
                    .ConfigureAwait(false))
            : FormInputFeedbackPromptBuilder.GetInputModeUnavailablePrompt(
                context.Contract,
                context.IsoCode,
                politenessLevel);

        await _speechService.SpeakSafeAsync(
                prompt,
                context.IsoCode,
                context.VoiceSettings,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task<string?> ResolveSilentInputFallbackLabelAsync(
        FormVoiceCommandGlobalCommandContext context,
        string variableId,
        PolitenessLevel politenessLevel,
        CancellationToken cancellationToken)
    {
        if (context.Interpreter is not IFormDialogPrivacyReadbackPolicy privacyReadbackPolicy)
        {
            return null;
        }

        var voiceInputAllowedResult = await privacyReadbackPolicy
            .IsVoiceInputAllowedAsync(variableId)
            .ConfigureAwait(false);
        return FormInputFeedbackPromptBuilder.GetSilentInputFallbackLabel(
            context.Contract,
            context.IsoCode,
            voiceInputAllowedResult.SilentInputFallback,
            politenessLevel);
    }
}
