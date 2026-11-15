namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Schreibt den ausdrücklich gewählten Unterbrechungsmodus über den Gerätezugang
/// und fordert die passende Zustandsansage an. Das leere Ergebnis lässt den
/// Formularablauf weiterlaufen. Eine eigene Sitzungsneustartanforderung entsteht nicht.
/// </summary>
internal sealed class FormVoiceCommandAllowBargeInHandler : IFormVoiceCommandGlobalCommandHandler
{
    private readonly IVoiceDialogSettingsOverrideStore _overrideStore;
    private readonly IFormVoiceCommandSpeechService _speechService;

    public FormVoiceCommandAllowBargeInHandler(
        IVoiceDialogSettingsOverrideStore overrideStore,
        IFormVoiceCommandSpeechService speechService)
    {
        _overrideStore = overrideStore ?? throw new ArgumentNullException(nameof(overrideStore));
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
    }

    public bool CanHandle(CommandId commandId)
        => commandId.Value is GlobalCommandIds.AllowBargeInOn or GlobalCommandIds.AllowBargeInOff;

    public async Task<VoiceCommandDialogResult?> HandleAsync(
        FormVoiceCommandGlobalCommandContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var target = string.Equals(context.CommandId, GlobalCommandIds.AllowBargeInOn, StringComparison.Ordinal);
        var current = context.VoiceSettings.GetAllowBargeIn();

        if (current == target)
        {
            var alreadyPrompt = target
                ? AllowBargeInPromptTextResolver.GetAlreadyEnabledPrompt(context.IsoCode)
                : AllowBargeInPromptTextResolver.GetAlreadyDisabledPrompt(context.IsoCode);

            await _speechService.SpeakSafeAsync(
                    alreadyPrompt,
                    context.IsoCode,
                    context.VoiceSettings,
                    cancellationToken)
                .ConfigureAwait(false);
            return null;
        }

        await _overrideStore.SetDeviceAllowBargeInOverrideAsync(target, cancellationToken).ConfigureAwait(false);

        var prompt = target
            ? AllowBargeInPromptTextResolver.GetEnabledPrompt(context.IsoCode, context.VoiceSettings.GetPolitenessLevel())
            : AllowBargeInPromptTextResolver.GetDisabledPrompt(context.IsoCode);

        await _speechService.SpeakSafeAsync(
                prompt,
                context.IsoCode,
                context.VoiceSettings,
                cancellationToken)
            .ConfigureAwait(false);

        return null;
    }
}
