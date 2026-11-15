namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet Übernehmen, Abschließen und Wiederholen den Befehlen der
/// Mehrfachauswahl zu. Die beiden Abschlussbefehle beenden die Auswahl
/// des Felds, nicht das gesamte Formular. Wiederholen fordert deren
/// erneutes Vorlesen an. Weitere globale Befehle werden hier nicht
/// abgebildet. Ihre Behandlung bleibt dem aufrufenden Ablauf überlassen.
/// </summary>
internal sealed class SetWorkflowGlobalCommandMapper
{
    public bool TryMap(string commandId, out SetWorkflowCommand command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandId);

        command = commandId switch
        {
            GlobalCommandIds.AcceptValue => SetWorkflowCommand.AcceptSelection,
            // Submit beendet in diesem Modus die Feldauswahl, nicht das Formular.
            GlobalCommandIds.Submit => SetWorkflowCommand.Complete,
            // Wiederholen fordert die aktuelle Auswahl erneut an und löscht keine Option.
            GlobalCommandIds.Repeat => SetWorkflowCommand.ReadbackSelection,
            _ => SetWorkflowCommand.None
        };

        return command is not SetWorkflowCommand.None;
    }
}
