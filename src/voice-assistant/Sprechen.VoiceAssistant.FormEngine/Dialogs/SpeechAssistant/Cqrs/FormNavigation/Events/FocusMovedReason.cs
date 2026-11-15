namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormNavigation.Events;

/// <summary>
/// Unterscheidet die Gründe einer protokollierten Fokusbewegung.
/// </summary>
public enum FocusMovedReason
{
    Initial,
    Next,
    Back,
    JumpTo,
    SetFocus,
    Error
}


