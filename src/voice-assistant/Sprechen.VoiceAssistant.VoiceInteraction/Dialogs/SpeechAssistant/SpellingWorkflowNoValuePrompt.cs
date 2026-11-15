namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Fordert die gemeinsame Ansage für einen leeren Buchstabierpuffer an.
/// Leerheitsprüfung und Registrierung des Reparaturversuchs bleiben beim Aufrufer.
/// </summary>
internal sealed class SpellingWorkflowNoValuePrompt
{
    private readonly IFormVoiceCommandSpeechService _speechService;

    public SpellingWorkflowNoValuePrompt(IFormVoiceCommandSpeechService speechService)
    {
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
    }

    public Task SpeakAsync(FormVoiceCommandContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        return _speechService.SpeakSafeAsync(
            FormSpellingWorkflowPromptBuilder.GetSpellingNoValuePrompt(
                context.Contract,
                context.IsoCode,
                context.VoiceSettings.GetPolitenessLevel()),
            context.IsoCode,
            context.VoiceSettings,
            cancellationToken);
    }
}
