namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Hält die zugeordnete Studienanwendung und ihr Formular für den direkten Einstieg
/// über einen Studienlink zusammen.
/// </summary>
public sealed record StudySession(string ApplicationId, string FormId);
