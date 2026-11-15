namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Fordert die modellierte Rückmeldung für einen leeren Ziffernpuffer an, damit dessen Bearbeitungsbefehle dieselbe Ansage verwenden.
/// </summary>
internal sealed class DigitsWorkflowNoValuePrompt
{
    private readonly IFormVoiceCommandSpeechService _speechService;

    public DigitsWorkflowNoValuePrompt(IFormVoiceCommandSpeechService speechService)
    {
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
    }

    public Task SpeakAsync(FormVoiceCommandContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        return _speechService.SpeakSafeAsync(
            FormDigitsPromptBuilder.GetDigitsNoValuePrompt(
                context.Contract,
                context.IsoCode,
                context.VoiceSettings.GetPolitenessLevel()),
            context.IsoCode,
            context.VoiceSettings,
            cancellationToken);
    }
}
