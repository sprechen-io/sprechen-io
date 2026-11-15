namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Löst ein Transkript über die globale Formularbefehlsgrammatik auf und ordnet
/// unterstützte Treffer dem Mengenablauf zu. Zusätzlich wird bestimmt, ob der
/// Befehl isoliert vorliegt. Optionsinhalte verarbeitet der aufrufende Eingabeweg.
/// </summary>
internal sealed class SetWorkflowCommandResolver
{
    private readonly SetWorkflowGlobalCommandMapper _globalCommandMapper;

    public SetWorkflowCommandResolver(SetWorkflowGlobalCommandMapper globalCommandMapper)
    {
        _globalCommandMapper = globalCommandMapper ?? throw new ArgumentNullException(nameof(globalCommandMapper));
    }

    public bool TryResolve(
        FormVoiceCommandContext context,
        string transcript,
        out SetWorkflowCommand command,
        out bool isIsolatedCommandSignal)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(transcript);

        command = SetWorkflowCommand.None;
        isIsolatedCommandSignal = false;
        var normalizedTranscript = transcript.Trim();

        if (FormGlobalCommandParser.TryParse(
                context.Contract,
                normalizedTranscript,
                context.IsoCode,
                context.VoiceSettings.GetPolitenessLevel(),
                out var globalCommandId,
                out _)
            && _globalCommandMapper.TryMap(globalCommandId, out command))
        {
            // Die isolierte Befehlsform getrennt bestimmen, damit der Aufrufer einen
            // Befehl innerhalb einer längeren Auswahläußerung anders behandeln kann.
            isIsolatedCommandSignal = FormGlobalCommandParser.IsIsolatedCommandSignal(
                context.Contract,
                normalizedTranscript,
                globalCommandId,
                context.IsoCode,
                context.VoiceSettings.GetPolitenessLevel());
            return true;
        }

        return false;
    }
}
