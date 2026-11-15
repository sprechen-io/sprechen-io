namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet unterstützte globale Befehle den Aktionen der Zifferneingabe zu.
/// Weiter, Abgeben und Übernehmen schließen die Eingabe ab. Wiederholen
/// liest die vorhandenen Ziffern vor.
/// </summary>
internal sealed class DigitsWorkflowGlobalCommandMapper
{
    public bool TryMap(string commandId, out DigitsWorkflowCommand command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandId);

        command = commandId switch
        {
            // Weiter beendet die Zifferneingabe unter Erhalt des Puffers.
            GlobalCommandIds.Next => DigitsWorkflowCommand.Continue,
            // Abgeben und Übernehmen schließen einen nichtleeren Ziffernpuffer ebenso ab.
            // Die nachfolgende Verarbeitung entscheidet über die Feldbestätigung.
            GlobalCommandIds.Submit => DigitsWorkflowCommand.Continue,
            GlobalCommandIds.AcceptValue => DigitsWorkflowCommand.Continue,
            // Wiederholen liest vor und löscht keinen Block. Erneutes Diktieren ist eine
            // getrennte Aktion. Zurück bleibt ein globaler Navigationsbefehl und wird
            // hier nicht als Löschen einer Ziffer abgebildet.
            GlobalCommandIds.Repeat => DigitsWorkflowCommand.ReadbackDigits,
            _ => DigitsWorkflowCommand.None
        };

        return command is not DigitsWorkflowCommand.None;
    }
}
