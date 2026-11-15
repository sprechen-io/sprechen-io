namespace Sprechen.VoiceAssistant.VoiceCommands.Dialogs.SpeechAssistant;

/// <summary>
/// Wiederholt beim Wiederholungsbefehl die letzte gespeicherte Sprachausgabe,
/// damit eine verpasste Rückmeldung erneut angehört werden kann. Ist der
/// Wiederholungspuffer leer, wird die Hilfe zum fokussierten Feld angefordert.
/// </summary>
internal sealed class FormVoiceCommandRepeatHandler : IFormVoiceCommandGlobalCommandHandler
{
    private readonly IFormVoiceCommandSpokenOutputReplayBuffer _replayBuffer;
    private readonly IFormVoiceCommandSpeechService _speechService;
    private readonly IFormVoiceCommandPromptService _promptService;

    public FormVoiceCommandRepeatHandler(
        IFormVoiceCommandSpokenOutputReplayBuffer replayBuffer,
        IFormVoiceCommandSpeechService speechService,
        IFormVoiceCommandPromptService promptService)
    {
        _replayBuffer = replayBuffer ?? throw new ArgumentNullException(nameof(replayBuffer));
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
        _promptService = promptService ?? throw new ArgumentNullException(nameof(promptService));
    }

    public bool CanHandle(CommandId commandId) => commandId == GlobalCommandIds.Repeat;

    public async Task<VoiceCommandDialogResult?> HandleAsync(
        FormVoiceCommandGlobalCommandContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var lastSpokenOutput = _replayBuffer.LastSpokenOutput;
        if (!string.IsNullOrWhiteSpace(lastSpokenOutput))
        {
            await _speechService.SpeakSafeAsync(
                    lastSpokenOutput,
                    context.IsoCode,
                    context.VoiceSettings,
                    cancellationToken)
                .ConfigureAwait(false);

            return null;
        }

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
