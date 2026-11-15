namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Bereitet schreibgeschützte Gruppeninhalte einmal je Gruppe vor: als letzten
/// Teil der Feldansage oder separat vor gesperrter Werteingabe. Die gemeinsame
/// Wertaufbereitung und Vorlagen bestimmen Inhalt und Vorlesefreigabe.
/// Gespeichert wird die Anforderung, nicht der Abschluss der Wiedergabe.
/// </summary>
internal sealed class FormVoiceCommandReadOnlyFieldSpeaker
{
    private readonly IFormVoiceCommandSpeechService _speechService;
    private readonly IFormVoiceCommandSpokenOutputReplayBuffer _replayBuffer;

    public FormVoiceCommandReadOnlyFieldSpeaker(
        IFormVoiceCommandSpeechService speechService,
        IFormVoiceCommandSpokenOutputReplayBuffer replayBuffer)
    {
        _speechService = speechService ?? throw new ArgumentNullException(nameof(speechService));
        _replayBuffer = replayBuffer ?? throw new ArgumentNullException(nameof(replayBuffer));
    }

    public async Task<string> ComposeFieldEntryPromptAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        bool announceReadOnlyFieldGroupContent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        var fieldPrompt = turnContext.FieldEntryPrompt ?? turnContext.Field.Label;
        var announcements = await TakeFieldGroupAnnouncementsAsync(
                turnContext, announceReadOnlyFieldGroupContent, cancellationToken)
            .ConfigureAwait(false);
        if (announcements.Length == 0)
        {
            return fieldPrompt;
        }

        // Aufnahmefragen ersetzen kein Wiederholungsziel. Nur die vorhandenen
        // Anleitungssegmente wie bei ihrer separaten Ausgabe vormerken.
        foreach (var announcement in announcements)
        {
            _replayBuffer.Record(announcement);
        }

        return string.Join(" ", new[] { fieldPrompt }.Concat(announcements));
    }

    public async Task AnnounceFieldGroupReadOnlyFieldsAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        bool announceReadOnlyFieldGroupContent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(turnContext);
        var announcements = await TakeFieldGroupAnnouncementsAsync(
                turnContext, announceReadOnlyFieldGroupContent, cancellationToken)
            .ConfigureAwait(false);
        foreach (var announcement in announcements)
        {
            await _speechService.SpeakSafeAsync(
                    announcement,
                    turnContext.Context.IsoCode,
                    turnContext.Context.VoiceSettings,
                    cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task<string[]> TakeFieldGroupAnnouncementsAsync(
        FormVoiceCommandFieldTurnContext turnContext,
        bool announceReadOnlyFieldGroupContent,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!announceReadOnlyFieldGroupContent)
        {
            return [];
        }

        var fieldGroupId = turnContext.Field.FieldGroupId;
        var projector = new FormReadOnlyFieldReadbackProjector(
            turnContext.Interpreter,
            turnContext.Interpreter,
            turnContext.Interpreter,
            turnContext.PrivacyReadbackPolicy,
            turnContext.Context.IsoCode);
        var readbacks = await projector.ProjectFieldGroupAnnouncementsAsync(fieldGroupId).ConfigureAwait(false);
        if (readbacks.Length == 0)
        {
            // Eine leere Projektion verbraucht keinen Merker.
            return [];
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (turnContext.StateTracker.HasAlreadyTaught($"{DialogTeachOnceTopics.ReadOnlyFieldGroupContent}:{fieldGroupId}"))
        {
            return [];
        }

        var announcements = new List<string>(readbacks.Length);
        var politenessLevel = turnContext.Context.VoiceSettings.GetPolitenessLevel();
        foreach (var readback in readbacks)
        {
            var prompt = readback.Status switch
            {
                FormSpokenValueProjectionStatus.Unavailable => FormReadOnlyFieldPromptBuilder.GetUnavailablePrompt(
                    turnContext.Context.Contract,
                    turnContext.Context.IsoCode,
                    politenessLevel,
                    readback.Label),
                FormSpokenValueProjectionStatus.FullValue
                    or FormSpokenValueProjectionStatus.Empty
                    or FormSpokenValueProjectionStatus.Masked
                    when !string.IsNullOrWhiteSpace(readback.SpokenValue)
                    => FormReadOnlyFieldPromptBuilder.GetAnnouncementPrompt(
                        turnContext.Context.Contract,
                        turnContext.Context.IsoCode,
                        politenessLevel,
                        readback.Label,
                        readback.SpokenValue),
                _ => FormReadOnlyFieldPromptBuilder.GetAnnouncementLabelOnlyPrompt(
                    turnContext.Context.Contract,
                    turnContext.Context.IsoCode,
                    politenessLevel,
                    readback.Label)
            };

            announcements.Add(prompt);
        }

        return [.. announcements];
    }
}
