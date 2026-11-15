namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Meldet die zugeordnete Studiensitzung an den Zustand, sobald der Dialoghost existiert.
/// </summary>
internal sealed record SpeechAssistantStudySessionBoundEvent(StudySession Session) : SpeechAssistantEvent;
