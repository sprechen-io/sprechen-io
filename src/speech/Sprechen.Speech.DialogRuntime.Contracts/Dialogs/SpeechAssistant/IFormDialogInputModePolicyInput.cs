namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft die Zulässigkeit des Eingabemodus und merkt, ob eine Ablehnung für das
/// aktuelle Feld bereits angesagt wurde. Der getrennte Vertrag hält
/// <see cref="IFormDialogValueCaptureInput"/> auf die Werterfassung beschränkt.
/// </summary>
public interface IFormDialogInputModePolicyInput
{
    /// <summary>
    /// Prüft ohne Zustandsänderung, ob der Eingabemodus aus dem modellierten Wortschatz
    /// am aktuell fokussierten Feld zulässig ist.
    /// </summary>
    Task<FormFieldInputModeAvailability> GetFocusedFieldInputModeAvailabilityAsync(string inputMode);

    /// <summary>
    /// Merkt für das fokussierte Feld und die aktuelle Zustandsrevision, dass die Ablehnung
    /// vor Aufnahmebeginn angesagt wurde. So wird sie höchstens einmal je Feldeintritt gesprochen.
    /// </summary>
    Task RecordDeniedFieldTurnAnnouncementAsync();

    /// <summary>
    /// Prüft, ob die Ablehnung vor Aufnahmebeginn für das aktuelle Feld und dessen Revision
    /// bereits angesagt wurde, damit eine erneute Verarbeitung sie nicht wiederholt.
    /// </summary>
    Task<bool> HasDeniedFieldTurnAnnouncementAsync();
}
