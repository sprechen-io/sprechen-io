using Sprechen.Speech.App.Contracts.Annotations;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Kennzeichnet das Navigationsziel eines Dialogergebnisses für die Auswahl des folgenden Assistentenablaufs.
/// </summary>
[ActionKind]
public enum VoiceCommandNavigationKind
{
    None,
    BackToFormSelection,
    BackToAppSelection,
    SwitchForm,
    ChangeLanguage,
    ChangePoliteness
}
