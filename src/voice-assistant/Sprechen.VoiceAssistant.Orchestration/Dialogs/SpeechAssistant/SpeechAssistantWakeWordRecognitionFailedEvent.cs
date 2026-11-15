namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Trägt Fehlercode und Wiederholbarkeit einer fehlgeschlagenen Aktivierungserkennung zum Reducer.
/// Dieser fordert die Rückkehr zur Bereitschaft an.
/// </summary>
public sealed record SpeechAssistantWakeWordRecognitionFailedEvent(string Code, bool Recoverable = true)
    : SpeechAssistantEvent;
