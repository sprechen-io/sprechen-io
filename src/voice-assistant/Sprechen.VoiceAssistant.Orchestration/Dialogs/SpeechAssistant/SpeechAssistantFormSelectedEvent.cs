namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Meldet das im Formularauswahldialog ausgewählte Formular an den Reducer.
/// </summary>
public sealed record SpeechAssistantFormSelectedEvent(
    FormId FormId,
    string? FormName) : SpeechAssistantEvent;


