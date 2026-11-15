using Sprechen.Speech.App.Contracts.Annotations;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Erkannte Handlungsabsichten im Dialog zur Auswahl einer Anwendung.
/// </summary>
[ActionKind]
public enum FormsCatalogIntent
{
    Unknown,

    /// <summary>
    /// Alle Anwendungen auflisten ("alle Anwendungen", "was gibt es").
    /// </summary>
    List,

    /// <summary>
    /// Anwendungen suchen ("suche nach Adresse", "finde Formular").
    /// </summary>
    Search,

    /// <summary>
    /// Anwendung per Name auswählen ("Kurzformular", "starte Adressformular").
    /// </summary>
    SelectByName,

    /// <summary>
    /// Anwendung per Nummer auswählen ("eins", "Nummer 2", "die zweite").
    /// </summary>
    SelectByNumber,

    /// <summary>
    /// Hilfe anfordern ("Hilfe", "was kann ich sagen").
    /// </summary>
    Help,

    /// <summary>
    /// Beschreibung einer Anwendung vorlesen ("beschreibe Kurzformular").
    /// </summary>
    Describe,

    /// <summary>
    /// Letzte Ansage wiederholen ("wiederholen", "nochmal").
    /// </summary>
    Repeat,

    /// <summary>
    /// Dialog abbrechen ("abbrechen", "zurück").
    /// </summary>
    Cancel,

    /// <summary>
    /// Sprache wechseln ("Sprache wechseln", "change language").
    /// </summary>
    ChangeLanguage,

    ConfirmYes,

    ConfirmNo,

    /// <summary>
    /// Öffnet einen ausdrücklich benannten, bereits angebotenen Kandidaten.
    /// </summary>
    OpenCandidate
}

