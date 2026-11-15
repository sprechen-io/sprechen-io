namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Übersetzt Bestätigungs-, Bearbeitungs- und Buchstabierbefehle in die entsprechenden Entscheidungen. Andere Kennungen werden als Befehlsaufträge weitergegeben.
/// </summary>
internal sealed class FormVoiceCommandConfirmationCommandDecisionMapper
{
    public ConfirmationDecision Map(string commandId, string? commandArgument)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandId);

        return commandId switch
        {
            GlobalCommandIds.AcceptValue => ConfirmationDecision.Continue(),
            GlobalCommandIds.EditValue => ConfirmationDecision.Edit(),
            GlobalCommandIds.InputModeSpelling => ConfirmationDecision.SwitchToSpelling(),
            _ => ConfirmationDecision.ExecuteCommand(commandId, commandArgument)
        };
    }
}
