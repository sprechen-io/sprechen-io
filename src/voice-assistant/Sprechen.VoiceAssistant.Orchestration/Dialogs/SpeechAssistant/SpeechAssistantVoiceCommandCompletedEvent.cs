namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Trägt das Ergebnis eines beendeten Sprachdialogs für ein Formular zum Reducer zurück,
/// damit dieser die Navigation und weitere Zustandsübergänge bestimmt.
/// </summary>
public sealed record SpeechAssistantVoiceCommandCompletedEvent(VoiceCommandDialogResult Result) : SpeechAssistantEvent;


