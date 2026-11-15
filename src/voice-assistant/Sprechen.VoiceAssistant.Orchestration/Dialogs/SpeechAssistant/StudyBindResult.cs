namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Verbindet die zugeordnete Studiensitzung mit dem Anzeigenamen für ihre Ansage,
/// damit Zustand und Rückmeldung denselben Einstieg beschreiben.
/// </summary>
internal sealed record StudyBindResult(StudySession Session, string ApplicationDisplayName);
