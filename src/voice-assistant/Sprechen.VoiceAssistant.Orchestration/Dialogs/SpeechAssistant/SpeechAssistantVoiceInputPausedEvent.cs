namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Meldet die angeforderte Pause der Spracheingabe an den Reducer,
/// damit dieser den Kontext für das spätere Fortsetzen sichern kann.
/// </summary>
public sealed record SpeechAssistantVoiceInputPausedEvent : SpeechAssistantEvent;
