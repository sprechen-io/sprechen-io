namespace Sprechen.Speech.App.Contracts.Services;

/// <summary>
/// Legt den Kontext einer Spracherfassung fest, etwa Aktivierung,
/// Formulareingabe, Kurzbestätigung oder Unterbrechung einer Ansage.
/// </summary>
public enum SpeechCaptureMode
{
    Listening = 0,
    PromptInterruption = 1,
    ListeningContinuation = 2,
    PromptInterruptionDetection = 3,
    ActivationListening = 4,
    ActivationListeningContinuation = 5,

    // Für kurze Bestätigungs- und Reparaturantworten wird die Schutzfrist nach
    // Wiedergabe verkürzt. Die Dauer zur Bestätigung eines Sprachbeginns bleibt
    // unverändert. Das verringert den vor der Antwort verworfenen Audioanteil.
    ShortConfirmation = 6
}
