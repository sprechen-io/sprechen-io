using Sprechen.Speech.App.Contracts.Annotations;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Unterscheidet die unterstützten Regelaktionen für Sichtbarkeit, Feldzugriff, Werte, Ansagekennungen und Navigation.
/// </summary>
[ActionKind]
internal enum FormActionKind
{
    SetVisible,
    SetEnabled,
    SetReadOnly,
    SetValue,
    ClearValue,
    CopyValue,
    Speak,
    NavigateToFieldGroup,
    FocusField
}
