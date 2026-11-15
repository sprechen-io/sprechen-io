namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet lokale Ziffernbefehlskennungen ihren Aktionen zu und trennt Vorlesen von dem ausdrücklich löschenden Neudiktieren.
/// </summary>
internal sealed class DigitsWorkflowLocalCommandMapper
{
    public bool TryMap(string commandId, out DigitsWorkflowCommand command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandId);

        command = commandId switch
        {
            DigitsWorkflowCommandIds.DeleteLastDigit => DigitsWorkflowCommand.RemoveLastDigit,
            DigitsWorkflowCommandIds.DeleteLastBlock => DigitsWorkflowCommand.RemoveLastBlock,
            // Wiederholen liest den gesamten Ziffernpuffer vor. Das erneute Diktieren
            // wird als eigene Aktion abgebildet und entfernt den letzten Block.
            DigitsWorkflowCommandIds.RepeatLastBlock => DigitsWorkflowCommand.ReadbackDigits,
            DigitsWorkflowCommandIds.RedictateLastBlock => DigitsWorkflowCommand.RedictateLastBlock,
            _ => DigitsWorkflowCommand.None
        };

        return command is not DigitsWorkflowCommand.None;
    }
}
