namespace Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

/// <summary>
/// Kennzeichnet einen Wiedergabefehler der Einstiegsfrage vor dem Antwortfenster, damit der Aufrufer
/// ihn von einem Fehler während der Antwortaufnahme unterscheidet und gezielt einmal wiederholen kann.
/// Der Fehler belegt weder vollständige Stille noch eine bestimmte technische Ursache.
/// </summary>
internal sealed class SessionEntryQuestionUnspokenException : Exception
{
    public SessionEntryQuestionUnspokenException(string isoCode, Exception innerException)
        : base($"The session-entry question for '{isoCode}' could not be spoken.", innerException)
    {
        IsoCode = isoCode;
    }

    public string IsoCode { get; }
}
