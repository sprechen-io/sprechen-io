namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Verfolgt Fehleransagen, Abbruchbestätigung und Abschluss des Formulardialogs.
/// Merkt außerdem je Sitzung, welche Bedienhinweise schon ausführlich erklärt wurden.
/// </summary>
public interface IFormVoiceCommandDialogStateTracker
{
    void BeginErrorPrompt();

    void BeginCancelConfirmation();

    void RecordSubmitted();

    void RecordCancelled();

    /// <summary>
    /// Registriert ein Thema in der Formularsitzung. Beim ersten Aufruf wird
    /// <c>false</c> zurückgegeben, damit der Aufrufer die vollständige Erklärung spricht.
    /// Spätere Aufrufe liefern <c>true</c> für eine kurze Erinnerung. Ein neuer Tracker
    /// beginnt ohne bereits erklärte Themen.
    /// </summary>
    bool HasAlreadyTaught(string teachingTopicId);
}
