namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Unterscheidet Gründe für eine erneute Eingabe im Formulardialog.
/// Die Kategorien beschreiben, was eine passende Reparaturansage berücksichtigen muss,
/// und fassen parserseitige Detailfehler mit gleicher Handlungsempfehlung zusammen.
/// </summary>
public enum FormRepairReason
{
    /// <summary>Kein verwertbares Transkript erreichte die Grammatik (Stille, nur Leerraum).</summary>
    NothingCaptured,

    /// <summary>Das Erkennungsergebnis wurde wegen zu geringer Konfidenz abgelehnt.</summary>
    ConfidenceRejected,

    /// <summary>Ein Transkript kam an, aber keine Grammatikregel traf.</summary>
    UnparsedUtterance,

    /// <summary>
    /// Ein bekanntes Antwortwort oder Kommando ist im aktuellen Dialogschritt nicht zulässig.
    /// Die Rückmeldung kann benennen, was erkannt wurde und welche Antwort hier erwartet wird.
    /// </summary>
    KnownVocabularyInvalidHere,

    /// <summary>Ein geschlossener Optionssatz galt und das Transkript traf keine seiner Optionen.</summary>
    NoMatchingOption,

    /// <summary>Das Transkript traf mehrere Optionen eines geschlossenen Satzes und keine gewinnt.</summary>
    AmbiguousChoice,

    /// <summary>Eine Ja/Nein-Antwort wurde erwartet, aber beide oder keine der beiden Bedeutungen erkannt.</summary>
    AmbiguousYesNo,

    /// <summary>Eine Zeichenakkumulation enthielt ein Token, das nicht in der Buchstabiergrammatik steht.</summary>
    UnknownSpellingToken,

    /// <summary>Der erkannte Wert liegt außerhalb des zulässigen Bereichs, etwa für Zahl, Datum oder Uhrzeit.</summary>
    ValueOutOfRange,

    /// <summary>Der Inhalt wurde erkannt, seine Ausdrucksform wird vom Feld aber nicht unterstützt.</summary>
    UnsupportedValueForm,
}
