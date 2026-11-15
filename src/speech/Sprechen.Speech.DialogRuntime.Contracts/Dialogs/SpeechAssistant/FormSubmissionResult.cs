namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Ergebnis eines Absendeversuchs: angenommen, mit Korrekturmöglichkeit blockiert
/// oder ohne auflösbares Fehlerziel beendet. So kann der Dialog passend fortgesetzt oder beendet werden.
/// </summary>
public enum FormSubmissionResult
{
    /// <summary>
    /// Alle sichtbaren Felder waren gültig. Das Formular wurde abgesendet.
    /// </summary>
    Submitted,

    /// <summary>
    /// Die Prüfung scheiterte. Ein bearbeitbares Fehlerfeld wurde fokussiert und eine
    /// Reparaturansage gesprochen. Der Dialog kann zur Korrektur fortgesetzt werden.
    /// </summary>
    BlockedAwaitingCorrection,

    /// <summary>
    /// Die Prüfung scheiterte ohne bearbeitbares Fehlerziel, etwa bei einer Pflichtbedingung
    /// an einem sichtbaren, schreibgeschützten Feld. Der Dialog muss enden, statt denselben
    /// Absendeversuch ohne Korrekturmöglichkeit zu wiederholen.
    /// </summary>
    BlockedUnresolvable,
}
