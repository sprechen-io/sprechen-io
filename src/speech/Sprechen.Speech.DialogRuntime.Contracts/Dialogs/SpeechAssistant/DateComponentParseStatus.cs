namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Ergebnis der Erkennung eines einzelnen Datumsbestandteils.
/// Fehlerarten erlauben eine gezielte Nachfrage nach Tag, Monat oder Jahr.
/// </summary>
public enum DateComponentParseStatus
{
    Parsed,

    NotRecognized,

    /// <summary>Die erkannte Zahl liegt außerhalb des zulässigen Bereichs: Tag 1-31 oder Monat 1-12.</summary>
    OutOfRange,

    /// <summary>Die Jahresangabe enthält weniger als vier Ziffern. Das vollständige Jahr wird nachgefragt.</summary>
    YearNotFourDigits,

    /// <summary>
    /// Eine vierstellige italienische Jahresangabe enthält ein mehrstelliges Transkriptwort.
    /// Weil sich eine fehlerhafte Zusammenziehung daraus nicht ausschließen lässt,
    /// soll das Jahr erneut ziffernweise genannt werden.
    /// </summary>
    YearUncertain
}
