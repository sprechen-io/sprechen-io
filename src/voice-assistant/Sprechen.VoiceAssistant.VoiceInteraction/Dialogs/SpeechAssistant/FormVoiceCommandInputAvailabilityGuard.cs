namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft vor der Werteaufnahme Spracheingabefreigabe und Tastaturmodus und stellt bei Ablehnung deren Rückmeldedaten für den zuständigen Ablauf bereit.
/// </summary>
internal sealed class FormVoiceCommandInputAvailabilityGuard
{
    private readonly FormVoiceCommandFieldErrorHandlingService _fieldErrorHandlingService;

    public FormVoiceCommandInputAvailabilityGuard(FormVoiceCommandFieldErrorHandlingService fieldErrorHandlingService)
    {
        _fieldErrorHandlingService = fieldErrorHandlingService ?? throw new ArgumentNullException(nameof(fieldErrorHandlingService));
    }

    /// <summary>
    /// Prüft die Spracheingabefreigabe und den Tastaturmodus des Felds und
    /// liefert bei Ablehnung eine Rückmeldebeschreibung. Diese Prüfmethode
    /// fordert selbst weder eine Ansage noch eine Aufnahme an.
    /// </summary>
    public async Task<FormVoiceCommandPreCaptureDenial?> EvaluateAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        InputMode expectedInputMode,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(turnContext);

        var voiceInputAllowedResult = await turnContext.PrivacyReadbackPolicy.IsVoiceInputAllowedAsync(turnContext.Field.VariableId)
            .ConfigureAwait(false);
        var voiceInputAllowed = voiceInputAllowedResult.Allowed;
        var silentInputFallback = voiceInputAllowedResult.SilentInputFallback;
        var keyboardModeActive = expectedInputMode == InputMode.Keyboard;
        var blocksVoiceValueCapture = !voiceInputAllowed || keyboardModeActive;
        if (!blocksVoiceValueCapture)
        {
            return null;
        }

        var issueCode = voiceInputAllowed
            ? FormVoiceCommandFieldErrorCodes.KeyboardModeActive
            : FormVoiceCommandFieldErrorCodes.VoiceInputForbidden;
        var silentInputLabel = FormInputFeedbackPromptBuilder.GetSilentInputFallbackLabel(
            turnContext.Context.Contract,
            turnContext.Context.IsoCode,
            silentInputFallback,
            turnContext.Context.VoiceSettings.GetPolitenessLevel());
        return new FormVoiceCommandPreCaptureDenial(
            issueCode,
            FormInputFeedbackPromptBuilder.GetVoiceInputForbiddenPrompt(
                turnContext.Context.Contract,
                turnContext.Context.IsoCode,
                turnContext.Context.VoiceSettings.GetPolitenessLevel(),
                silentInputLabel),
            expectedInputMode.ToWireValue());
    }

    public Task TrackDenialAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        FormVoiceCommandPreCaptureDenial denial)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        ArgumentNullException.ThrowIfNull(denial);
        return _fieldErrorHandlingService.TrackPreCaptureRejectionAsync(
            turnContext,
            denial.IssueCode,
            denial.ExpectedInputMode);
    }

    public Task AnnounceDenialAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        FormVoiceCommandPreCaptureDenial denial,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        ArgumentNullException.ThrowIfNull(denial);
        return _fieldErrorHandlingService.SpeakPreCaptureDenialAsync(
            turnContext,
            denial.Prompt,
            cancellationToken);
    }
}
