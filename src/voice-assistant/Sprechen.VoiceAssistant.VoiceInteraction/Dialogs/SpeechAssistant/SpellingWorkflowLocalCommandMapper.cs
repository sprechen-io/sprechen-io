namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet unterstützte lokale Buchstabierbefehle den internen Aktionen zu und weist unbekannte Kennungen zurück.
/// </summary>
internal sealed class SpellingWorkflowLocalCommandMapper
{
    public bool TryMap(string commandId, out SpellingWorkflowCommand command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandId);

        command = commandId switch
        {
            SpellingWorkflowCommandIds.RemoveLastCharacter => SpellingWorkflowCommand.RemoveLastCharacter,
            SpellingWorkflowCommandIds.RemoveLastBlock => SpellingWorkflowCommand.RemoveLastBlock,
            SpellingWorkflowCommandIds.ClearBuffer => SpellingWorkflowCommand.ClearBuffer,
            SpellingWorkflowCommandIds.Readback => SpellingWorkflowCommand.ReadbackCharacters,
            _ => SpellingWorkflowCommand.None
        };

        return command is not SpellingWorkflowCommand.None;
    }
}
