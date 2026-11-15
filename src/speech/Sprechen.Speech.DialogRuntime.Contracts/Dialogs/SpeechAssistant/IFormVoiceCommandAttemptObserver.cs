namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Optionaler Beobachter vor jedem Erfassungsversuch. Der Formular-Aufnahmedienst
/// benachrichtigt ihn bei Wiederholungen nach fehlender Eingabe, der Bestätigungsfluss
/// bei seinen eigenen Nachfragen. So kann gesprochene Rückmeldung auf Timeout
/// oder eine nicht erkannte Antwort reagieren.
/// </summary>
public interface IFormVoiceCommandAttemptObserver
{
    /// <summary>
    /// Reagiert vor Beginn des Versuchs. Der Aufrufer wartet auf die Aufgabe, damit
    /// etwa eine gesprochene Nachfrage vor dem Öffnen des Erfassungsfensters endet.
    /// Asynchrones Warten vermeidet das Blockieren der Browserlaufzeit. Das Token
    /// überträgt den Dialogabbruch auf diese Reaktion.
    /// </summary>
    Task OnAttemptStartingAsync(FormVoiceCommandAttempt attempt, CancellationToken cancellationToken);
}
