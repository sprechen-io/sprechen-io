namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Fordert eine nach Kategorien gegliederte und je Kategorie begrenzte
/// Ansage verfügbarer Formularbefehle an. Die Verarbeitung liefert kein
/// Endergebnis und ändert selbst keinen Formularzustand.
/// </summary>
internal sealed class FormVoiceCommandCommandListHandler : IFormVoiceCommandGlobalCommandHandler
{
    private const int PerGroupSpokenCommandCap = 5;

    private readonly IFormVoiceCommandSpeechService _speechService;
    private readonly FormVoiceCommandAvailableCommandsPromptBuilder _availableCommandsPromptBuilder;

    public FormVoiceCommandCommandListHandler(IFormVoiceCommandSpeechService speechService)
        : this(speechService, new FormVoiceCommandAvailableCommandsPromptBuilder())
    {
    }

    public FormVoiceCommandCommandListHandler(
        IFormVoiceCommandSpeechService speechService,
        FormVoiceCommandAvailableCommandsPromptBuilder availableCommandsPromptBuilder)
    {
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
        _availableCommandsPromptBuilder = availableCommandsPromptBuilder
            ?? throw new ArgumentNullException(nameof(availableCommandsPromptBuilder));
    }

    public bool CanHandle(CommandId commandId) => commandId == GlobalCommandIds.CommandList;

    public async Task<VoiceCommandDialogResult?> HandleAsync(
        FormVoiceCommandGlobalCommandContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        var politenessLevel = context.VoiceSettings.GetPolitenessLevel();

        var readback = _availableCommandsPromptBuilder.BuildReadback(
            context.Contract,
            context.IsoCode,
            politenessLevel,
            PerGroupSpokenCommandCap);

        var groups = readback.Groups
            .Select(group => (group.Category, group.ShownLabels))
            .ToList();

        var prompt = FormCommandListPromptBuilder.BuildPrompt(
            context.Contract,
            groups,
            readback.TotalCount,
            readback.Truncated,
            context.IsoCode,
            politenessLevel);

        await _speechService.SpeakSafeAsync(prompt, context.IsoCode, context.VoiceSettings, cancellationToken)
            .ConfigureAwait(false);

        return null;
    }
}
