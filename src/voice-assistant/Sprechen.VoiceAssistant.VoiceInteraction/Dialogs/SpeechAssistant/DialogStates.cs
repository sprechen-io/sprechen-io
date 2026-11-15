namespace Sprechen.VoiceAssistant.VoiceInteraction.Dialogs.SpeechAssistant;

/// <summary>
/// Hält die Zustandsnamen für die Telemetrie des sprachgesteuerten Formularablaufs bereit.
/// </summary>
internal static class DialogStates
{
    public const string Init = "INIT";
    public const string Prompting = "PROMPTING";
    public const string Listening = "LISTENING";
    public const string Interpreting = "INTERPRETING";
    public const string Validating = "VALIDATING";
    public const string Confirmation = "CONFIRMATION";
    public const string ErrorPrompt = "ERROR_PROMPT";
    public const string CancelConfirmation = "CANCEL_CONFIRMATION";
    public const string Repair = "REPAIR";
}
