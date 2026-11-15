namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft zunächst den lokalen Buchstabierwortschatz und danach globale Befehle. Ein lokaler Treffer beendet die Auflösung auch bei nicht unterstützter Zuordnung.
/// </summary>
internal sealed class SpellingWorkflowCommandResolver
{
    private readonly SpellingWorkflowLocalCommandMapper _localCommandMapper;
    private readonly SpellingWorkflowGlobalCommandMapper _globalCommandMapper;

    public SpellingWorkflowCommandResolver(
        SpellingWorkflowLocalCommandMapper localCommandMapper,
        SpellingWorkflowGlobalCommandMapper globalCommandMapper)
    {
        _localCommandMapper = localCommandMapper ?? throw new ArgumentNullException(nameof(localCommandMapper));
        _globalCommandMapper = globalCommandMapper ?? throw new ArgumentNullException(nameof(globalCommandMapper));
    }

    /// <summary>
    /// Löst einen Buchstabierbefehl auf und meldet bei Erfolg zusätzlich, ob die
    /// Äußerung als isolierter Befehl erkannt wurde, damit begleitender Inhalt getrennt behandelt werden kann.
    /// </summary>
    public bool TryResolve(
        FormVoiceCommandContext context,
        string transcript,
        out SpellingWorkflowCommand command,
        out bool isIsolatedCommandSignal,
        out string? globalCommandId)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(transcript);

        command = SpellingWorkflowCommand.None;
        // Ein Treffer der lokalen Grammatik gilt für diesen Auflösungspfad als isoliert.
        isIsolatedCommandSignal = true;
        globalCommandId = null;
        var normalizedTranscript = transcript.Trim();

        if (SpellingWorkflowCommandParser.TryParse(
                normalizedTranscript,
                context.IsoCode,
                context.VoiceSettings.GetPolitenessLevel(),
                out var spellingWorkflowCommandId))
        {
            return _localCommandMapper.TryMap(spellingWorkflowCommandId, out command);
        }

        if (FormGlobalCommandParser.TryParse(
                context.Contract,
                normalizedTranscript,
                context.IsoCode,
                context.VoiceSettings.GetPolitenessLevel(),
                out var matchedGlobalCommandId,
                out _)
            && _globalCommandMapper.TryMap(matchedGlobalCommandId, out command))
        {
            globalCommandId = matchedGlobalCommandId;
            isIsolatedCommandSignal = FormGlobalCommandParser.IsIsolatedCommandSignal(
                context.Contract,
                normalizedTranscript,
                matchedGlobalCommandId,
                context.IsoCode,
                context.VoiceSettings.GetPolitenessLevel());
            return true;
        }

        return false;
    }
}
