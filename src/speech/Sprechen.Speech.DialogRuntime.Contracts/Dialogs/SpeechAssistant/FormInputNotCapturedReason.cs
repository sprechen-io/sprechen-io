namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Grund, warum ein Transkript nicht als Feldwert übernommen wurde.
/// Fehlendes Eingabeziel, leerer Wert und Typunverträglichkeit bleiben damit
/// für Rückmeldung und Diagnose unterscheidbar.
/// </summary>
public enum FormInputNotCapturedReason
{
    /// <summary>Der Wert wurde erfasst. Kein Grund greift.</summary>
    None = 0,

    /// <summary>Es war kein fokussiertes Feld als Eingabeziel verfügbar.</summary>
    NoInputTarget,

    /// <summary>Der zusammengesetzte Wert war nach der Normalisierung leer oder nur Leerraum.</summary>
    BlankValue,

    /// <summary>Der Wert kann kein Wert des gebundenen Feldtyps sein.</summary>
    ValueTypeMismatch
}
