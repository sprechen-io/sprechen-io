namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet unterstützte globale Befehle dem Buchstabiermodus zu.
/// Spracheingabe, Absenden, Weiter und Übernehmen beenden die Eingabe
/// dieses Felds. Wiederholen fordert den vorhandenen Wert erneut an.
/// Ob danach eine Bestätigung nötig ist, entscheidet der weitere Feldablauf.
/// </summary>
internal sealed class SpellingWorkflowGlobalCommandMapper
{
    public bool TryMap(string commandId, out SpellingWorkflowCommand command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandId);

        command = commandId switch
        {
            // Der Wechsel zur Spracheingabe beendet hier die Buchstabiereingabe mit erhaltenem Puffer.
            GlobalCommandIds.InputModeVoice => SpellingWorkflowCommand.Complete,
            GlobalCommandIds.Submit => SpellingWorkflowCommand.Complete,
            // Weiter beendet hier die Eingabe des aktuellen Werts und führt nicht
            // unmittelbar eine Feldnavigation aus.
            GlobalCommandIds.Next => SpellingWorkflowCommand.Complete,
            GlobalCommandIds.AcceptValue => SpellingWorkflowCommand.Complete,
            // Wiederholen fordert den vorhandenen Wert an und verändert den Puffer nicht.
            GlobalCommandIds.Repeat => SpellingWorkflowCommand.ReadbackCharacters,
            _ => SpellingWorkflowCommand.None
        };

        return command is not SpellingWorkflowCommand.None;
    }
}
