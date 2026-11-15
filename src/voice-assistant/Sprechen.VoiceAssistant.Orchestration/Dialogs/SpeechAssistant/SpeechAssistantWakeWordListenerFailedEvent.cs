namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Meldet einen fehlgeschlagenen Lauf der Aktivierungsworterkennung an den Assistentenreducer.
/// </summary>
public sealed record SpeechAssistantWakeWordListenerFailedEvent : SpeechAssistantEvent;
