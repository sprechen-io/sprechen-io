namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Trägt die beim Pausieren zurückgegebenen Formularwerte zum Reducer, damit sie für das Fortsetzen
/// verfügbar bleiben. Die Pausenphase übernimmt sie. Andere Phasen ignorieren die Meldung, weil ein
/// auslaufender Dialogeffekt sie verspätet liefern kann.
/// </summary>
public sealed record SpeechAssistantVoiceCommandPausedEvent(VoiceCommandDialogResult Result) : SpeechAssistantEvent;
