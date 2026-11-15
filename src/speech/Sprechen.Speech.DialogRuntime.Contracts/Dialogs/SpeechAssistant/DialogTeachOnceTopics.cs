namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Kennungen für Hinweise, die in einer Formularsitzung zunächst ausführlich
/// und später verkürzt oder nicht erneut gesprochen werden. Der Zustand liegt bei
/// <see cref="IFormVoiceCommandDialogStateTracker.HasAlreadyTaught(string)"/>.
/// </summary>
public static class DialogTeachOnceTopics
{
    /// <summary>Einführung in phonetisches Alphabet und Kommandos des Buchstabiermodus.</summary>
    public const string SpellingModeTail = "spellingModeTail";

    /// <summary>
    /// Hinweis der Feldhilfe auf das Kommando <c>commandList</c>, höchstens einmal
    /// je Feld und Sitzung. Der Schlüssel <c>helpCommandsPointer:{fieldId}</c> erlaubt
    /// die erneute Einführung an einem anderen Feld.
    /// </summary>
    public const string HelpCommandsPointer = "helpCommandsPointer";

    /// <summary>
    /// Hinweis der Feldhilfe auf den erforderlichen Aktivierungswort-Anker für Kommandos.
    /// Er wird mit <c>helpCommandAnchor:{fieldId}</c> wie <see cref="HelpCommandsPointer"/>
    /// pro Feld und Sitzung geführt, damit die Regel bei einem Feldwechsel erneut hörbar wird.
    /// </summary>
    public const string HelpCommandAnchor = "helpCommandAnchor";

    /// <summary>Hinweis zur manuellen Fortsetzung nach einer Wertübernahme ohne automatisches Fortschreiten.</summary>
    public const string ManualContinuationTail = "manualContinuationTail";

    /// <summary>
    /// Automatische Ansage schreibgeschützter Anzeigefelder beim ersten Eintritt
    /// in ihre Feldgruppe. Der Schlüssel <c>readOnlyFieldGroupContent:{fieldGroupId}</c>
    /// verhindert Wiederholungen innerhalb derselben Formularsitzung. Späteres Vorlesen erfolgt auf Anfrage.
    /// </summary>
    public const string ReadOnlyFieldGroupContent = "readOnlyFieldGroupContent";

    /// <summary>
    /// Erklärt am ersten Datumsfeld der Sitzung die Aufteilung in Tag, Monat und Jahr.
    /// Spätere Datumsfelder erhalten nur ein Beispiel, solange kein Erkennungsfehler
    /// eine erneute Strukturerklärung erforderlich macht.
    /// </summary>
    public const string DateStructure = "dateStructure";

    /// <summary>
    /// Erklärt am ersten Mehrfachauswahlfeld, dass Optionen einzeln genannt und
    /// mit dem Abschlusswort beendet werden. Später folgt diese Erklärung nur bei
    /// entsprechendem Erkennungsfehler erneut. Die Rückmeldung zur laufenden Auswahl
    /// bleibt davon unabhängig.
    /// </summary>
    public const string SetAccumulation = "setAccumulation";

    /// <summary>
    /// Erklärt am ersten Zeitfeld die verwendete Uhrzeitkonvention: zwölf Stunden
    /// mit AM/PM für Englisch, 24 Stunden für Deutsch, Französisch, Spanisch und Italienisch.
    /// Spätere Felder erhalten nur ein Beispiel, sofern kein Erkennungsfehler eine erneute Erklärung verlangt.
    /// </summary>
    public const string TimeStructure = "timeStructure";

    /// <summary>
    /// Macht am ersten geeigneten Freitext- oder Codefeld auf den freiwilligen
    /// Buchstabiermodus aufmerksam. Spätere geeignete Felder erhalten einen kürzeren Hinweis.
    /// Das Umschalten bleibt eine ausdrückliche und umkehrbare Nutzerentscheidung.
    /// </summary>
    public const string SpellingOverrideAvailable = "spellingOverrideAvailable";
}
