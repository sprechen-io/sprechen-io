namespace Sprechen.Mda.Contracts.V1.Catalog;

/// <summary>
/// Kanonische Intent-Kennungen des Auswahldialogs für Schema, Vorprüfung und Laufzeit.
/// </summary>
public static class CatalogVoiceIntentId
{
    /// <summary>Bestätigungs-Intent für Ja-Antworten.</summary>
    public const string ConfirmYes = "confirmYes";

    /// <summary>Bestätigungs-Intent für Nein-Antworten.</summary>
    public const string ConfirmNo = "confirmNo";

    /// <summary>Intent zum Abbrechen des laufenden Auswahldialogs.</summary>
    public const string Cancel = "cancel";

    /// <summary>Intent zum Wechseln der Sitzungssprache.</summary>
    public const string ChangeLanguage = "changeLanguage";

    /// <summary>Intent zum Wiederholen der vorherigen Ansage.</summary>
    public const string Repeat = "repeat";

    /// <summary>Intent zur Frage nach verfügbaren Kommandos.</summary>
    public const string Help = "help";

    /// <summary>Intent zum Aufzählen aller verfügbaren Einträge.</summary>
    public const string List = "list";

    /// <summary>Intent zur Auswahl per Ordinalzahl oder Zahlenpräfix.</summary>
    public const string SelectByNumber = "selectByNumber";

    /// <summary>Intent zur Katalogsuche per Suchbegriff.</summary>
    public const string Search = "search";

    /// <summary>Intent zur Beschreibung eines Eintrags.</summary>
    public const string Describe = "describe";

    /// <summary>
    /// Intent zur ausdrücklichen Auswahl eines beibehaltenen Kandidaten anhand seines Namens.
    /// </summary>
    public const string OpenCandidate = "openCandidate";
}
