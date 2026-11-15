using System.Collections.Immutable;

namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Fordert eine Bestätigung des Formularwechsels an und liefert nur bei Zustimmung das entsprechende Wechselergebnis.
/// </summary>
internal sealed class FormVoiceCommandSwitchFormConfirmationFlow
{
    private readonly IFormVoiceCommandSpeechService _speechService;

    public FormVoiceCommandSwitchFormConfirmationFlow(IFormVoiceCommandSpeechService speechService)
    {
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
    }

    public async Task<VoiceCommandDialogResult?> ConfirmAsync(
        FormVoiceCommandGlobalCommandContext context,
        FormVoiceCommandSwitchFormTarget target,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(target);

        var confirm = await _speechService.ConfirmYesNoAsync(
                context.CaptureSettings,
                FormNavigationSwitchPromptBuilder.GetSwitchFormConfirmationPrompt(
                    context.Contract,
                    context.IsoCode,
                    target.DisplayName,
                    context.VoiceSettings.GetPolitenessLevel()),
                context.IsoCode,
                new SpeechCaptureTelemetryContext(context.Session),
                cancellationToken)
            .ConfigureAwait(false);

        if (confirm is not true)
        {
            return null;
        }

        return VoiceCommandDialogResult.SwitchForm(
            context.Contract.ApplicationId,
            context.CurrentForm.FormId,
            target.Form.FormId,
            (await context.Interpreter.GetValuesSnapshotAsync().ConfigureAwait(false))
                .ToImmutableDictionary(StringComparer.Ordinal));
    }
}
