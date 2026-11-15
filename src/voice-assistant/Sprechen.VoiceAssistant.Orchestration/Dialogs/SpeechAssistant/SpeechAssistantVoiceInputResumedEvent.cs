namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Fordert das Fortsetzen nach einer Pause an, damit der Reducer den gesicherten Dialogkontext wieder
/// aufnimmt.
/// </summary>
public sealed record SpeechAssistantVoiceInputResumedEvent : SpeechAssistantEvent;
