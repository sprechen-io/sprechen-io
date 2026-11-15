namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Meldet eine fehlgeschlagene Änderung der Anrede an den Reducer,
/// damit er zur Bereitschaft für das Aktivierungswort zurückkehren kann.
/// </summary>
public sealed record SpeechAssistantVoiceCommandPolitenessChangeFailedEvent : SpeechAssistantEvent;
