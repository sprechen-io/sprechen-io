namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Unterscheidet die aktiven Dialogzweige des Assistenten unabhängig von ihrer Lebenszyklusphase.
/// </summary>
public enum SpeechAssistantDialogKind
{
    FormsCatalog,
    FormSelection,
    VoiceCommand
}



