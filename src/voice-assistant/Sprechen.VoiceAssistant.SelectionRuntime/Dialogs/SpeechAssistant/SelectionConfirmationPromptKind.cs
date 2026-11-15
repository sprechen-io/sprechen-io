namespace Sprechen.VoiceAssistant.SelectionRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Unterscheidet die vorgesehenen Bestätigungsansagen, damit die Ansagefunktion
/// Kandidat und Bestätigungsstufe passend berücksichtigen kann.
/// </summary>
public enum SelectionConfirmationPromptKind
{
    /// <summary>
    /// Fordert die gewöhnliche erste Bestätigung des Kandidaten an.
    /// </summary>
    InitialConfirmation = 0,

    /// <summary>
    /// Fordert die erste Bestätigung bei unsicherer Erkennung der Auswahl an.
    /// </summary>
    LowConfidenceConfirmation = 1,

    /// <summary>
    /// Fordert die Autorisierung durch einen Öffnungsbefehl mit exaktem Kandidatennamen oder durch
    /// Auswahl über die stabile Nummer an.
    /// </summary>
    ExplicitAuthorization = 2,

    /// <summary>
    /// Fordert den einmaligen zusätzlichen Prüfversuch bei fehlendem Vertrauenswert an.
    /// </summary>
    VerificationUnavailable = 3,

    /// <summary>
    /// Fordert erneute Auswahl nach nicht autorisiertem Kandidaten an.
    /// </summary>
    NotOpenedReselect = 4,

    Cancellation = 5,

    /// <summary>
    /// Fordert die Wiederholung der aktiven Bestätigungsansage an.
    /// </summary>
    ActiveStage = 6,

    /// <summary>
    /// Fordert den vollständigen Ausgangsbestand mit anschließender Ansage der aktiven Stufe an.
    /// </summary>
    ActiveStageList = 7,

    /// <summary>
    /// Fordert die systembezogene Beendigung bei weiterhin fehlendem Vertrauenswert an.
    /// </summary>
    VerificationFailedTerminal = 8,

    /// <summary>
    /// Fordert eine neutrale Abschlussansage nach zu vielen erfolglosen
    /// Bestätigungsversuchen an.
    /// </summary>
    RepairExhaustedTerminal = 9,

    /// <summary>
    /// Fordert die modellierte Hilfe für die aktive Bestätigungsstufe an.
    /// </summary>
    ContextualHelp = 10,

    /// <summary>
    /// Fordert die kurze Kandidatenerinnerung nach einem Fenster ohne verwendbare Eingabe an.
    /// </summary>
    NoInputReminder = 11
}
