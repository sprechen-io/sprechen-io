namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Löst lokale Ziffernbefehle vor globalen Befehlen auf und gibt deren isoliertes Auftreten mit, damit der Ablauf Befehl und beigefügten Inhalt unterscheiden kann.
/// </summary>
internal sealed class DigitsWorkflowCommandResolver
{
    private readonly DigitsWorkflowLocalCommandMapper _localCommandMapper;
    private readonly DigitsWorkflowGlobalCommandMapper _globalCommandMapper;

    public DigitsWorkflowCommandResolver(
        DigitsWorkflowLocalCommandMapper localCommandMapper,
        DigitsWorkflowGlobalCommandMapper globalCommandMapper)
    {
        _localCommandMapper = localCommandMapper ?? throw new ArgumentNullException(nameof(localCommandMapper));
        _globalCommandMapper = globalCommandMapper ?? throw new ArgumentNullException(nameof(globalCommandMapper));
    }

    /// <summary>
    /// Löst zuerst einen lokalen Ziffernbefehl auf und verwendet nur ohne lokalen Treffer
    /// den globalen Parser. Das Ergebnis trägt mit, ob die Äußerung als isoliert eingeordnet wurde.
    /// </summary>
    public bool TryResolve(
        FormVoiceCommandContext context,
        string transcript,
        out DigitsWorkflowCommand command,
        out bool isIsolatedCommandSignal,
        out string? globalCommandId)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(transcript);

        command = DigitsWorkflowCommand.None;
        isIsolatedCommandSignal = true;
        globalCommandId = null;
        var normalizedTranscript = transcript.Trim();

        if (DigitsWorkflowCommandParser.TryParse(
                normalizedTranscript,
                context.IsoCode,
                context.VoiceSettings.GetPolitenessLevel(),
                out var digitsWorkflowCommandId))
        {
            return _localCommandMapper.TryMap(digitsWorkflowCommandId, out command);
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
