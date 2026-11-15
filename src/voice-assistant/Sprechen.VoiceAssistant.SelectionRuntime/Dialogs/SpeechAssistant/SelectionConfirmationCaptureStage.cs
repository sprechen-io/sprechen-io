namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Unterscheidet die kandidatenbezogenen Erfassungsstufen der Bestätigung, damit zulässige Antworten
/// und Rückmeldungen zur aktuellen Autorisierung passen.
/// </summary>
public enum SelectionConfirmationCaptureStage
{
    /// <summary>
    /// Kennzeichnet die erste Ja-Nein-Bestätigung des aufgelösten Kandidaten.
    /// </summary>
    Initial = 0,

    /// <summary>
    /// Kennzeichnet den einmaligen zusätzlichen Autorisierungsversuch nach einem Ja unterhalb der
    /// Vertrauensschwelle.
    /// </summary>
    ExplicitAuthorization = 1,

    /// <summary>
    /// Kennzeichnet den einmaligen zusätzlichen Autorisierungsversuch bei fehlendem Vertrauenswert.
    /// </summary>
    ConfidenceFaultRetry = 2
}
