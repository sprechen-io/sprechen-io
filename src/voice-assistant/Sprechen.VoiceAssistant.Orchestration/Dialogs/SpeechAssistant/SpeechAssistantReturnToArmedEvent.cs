namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Fordert die Rückkehr zur Bereitschaft für das Aktivierungswort an, damit der Reducer
/// den Bereitschaftston und die Erkennung als nächste Effekte einplanen kann.
/// </summary>
public sealed record SpeechAssistantReturnToArmedEvent : SpeechAssistantEvent;


