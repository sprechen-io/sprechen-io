namespace Sprechen.VoiceAssistant.VoiceCommands.Dialogs.SpeechAssistant;

/// <summary>
/// Fordert beim Hilfebefehl die Spracherklärung zum fokussierten
/// Formularfeld über den zuständigen Ansagedienst an.
/// </summary>
internal sealed class FormVoiceCommandHelpHandler : IFormVoiceCommandGlobalCommandHandler
{
    private readonly IFormVoiceCommandPromptService _promptService;

    public FormVoiceCommandHelpHandler(IFormVoiceCommandPromptService promptService)
    {
        _promptService = promptService ?? throw new ArgumentNullException(nameof(promptService));
    }

    public bool CanHandle(CommandId commandId) => commandId == GlobalCommandIds.Help;

    public async Task<VoiceCommandDialogResult?> HandleAsync(
        FormVoiceCommandGlobalCommandContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        await _promptService.SpeakHelpForFocusedFieldAsync(
                context.Interpreter,
                context.StateTracker,
                context.IsoCode,
                context.VoiceSettings,
                cancellationToken)
            .ConfigureAwait(false);

        return null;
    }
}

