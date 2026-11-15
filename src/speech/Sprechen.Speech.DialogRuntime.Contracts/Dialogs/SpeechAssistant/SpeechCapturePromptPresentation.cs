namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Bestimmt, ob die Erfassung mit einer gesprochenen Ansage oder direkt mit
/// dem Hörsignal vorbereitet wird.
/// </summary>
public enum SpeechCapturePromptPresentation
{
    Spoken,
    ListeningCueOnly,
}
