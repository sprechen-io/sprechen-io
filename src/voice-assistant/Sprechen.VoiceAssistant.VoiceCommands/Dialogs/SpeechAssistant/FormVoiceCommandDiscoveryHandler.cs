namespace Sprechen.VoiceAssistant.VoiceCommands.Dialogs.SpeechAssistant;

/// <summary>
/// Beantwortet Befehle zur Feld- oder Feldgruppenübersicht mit
/// Sprachausgaben aus der aktuellen Feld- und Feldgruppenübersicht.
/// </summary>
internal sealed class FormVoiceCommandDiscoveryHandler : IFormVoiceCommandGlobalCommandHandler
{
    private readonly IFormVoiceCommandSpeechService _speechService;

    public FormVoiceCommandDiscoveryHandler(IFormVoiceCommandSpeechService speechService)
    {
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
    }

    public bool CanHandle(CommandId commandId)
        => commandId.Value is GlobalCommandIds.FieldGroupList or GlobalCommandIds.FieldList;

    public async Task<VoiceCommandDialogResult?> HandleAsync(
        FormVoiceCommandGlobalCommandContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var discovery = await context.Interpreter.GetDiscoverySnapshotAsync().ConfigureAwait(false);

        var prompt = string.Equals(context.CommandId, GlobalCommandIds.FieldGroupList, StringComparison.Ordinal)
            ? FormStatusPromptBuilder.BuildFieldGroupListPrompt(
                discovery,
                context.Contract,
                context.IsoCode,
                context.VoiceSettings)
            : FormStatusPromptBuilder.BuildFieldListPrompt(
                discovery,
                context.Contract,
                context.IsoCode,
                context.VoiceSettings);

        await _speechService.SpeakSafeAsync(
                prompt,
                context.IsoCode,
                context.VoiceSettings,
                cancellationToken)
            .ConfigureAwait(false);

        return null;
    }
}
