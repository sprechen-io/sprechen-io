namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>Grund für die erneute Nachfrage nach einem Datumsbestandteil.</summary>
public enum DateComponentReaskReason
{
    /// <summary>Für den Datumsbestandteil wurde kein verwertbarer Wert erkannt.</summary>
    NotRecognized,

    /// <summary>Eine Zahl wurde erkannt, liegt aber außerhalb des zulässigen Bereichs.</summary>
    OutOfRange,

    /// <summary>Das Jahr muss vollständig vierstellig genannt werden. Das Jahrhundert wird nicht ergänzt.</summary>
    YearNotFourDigits,

    /// <summary>
    /// Die Erkennung einer italienischen Jahresangabe enthält ein mehrstelliges
    /// Transkriptwort. Eine erneute ziffernweise Antwort soll die Unsicherheit klären.
    /// </summary>
    YearUncertain,

    /// <summary>Tag, Monat und Jahr bilden kein echtes Kalenderdatum. Der Tag wird nachgefragt.</summary>
    ImpossibleDate
}
