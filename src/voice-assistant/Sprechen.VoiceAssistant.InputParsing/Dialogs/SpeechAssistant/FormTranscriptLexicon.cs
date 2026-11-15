namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Bildet das eingebettete Transkriptlexikon als Datenträger ab,
/// damit Parser und Buchstabierausgabe modellierte Wörter gemeinsam einlesen können.
/// </summary>
internal sealed class FormTranscriptLexicon
{
    public Dictionary<string, string> SpellingTokens { get; init; } = new(StringComparer.Ordinal);

    // Benannte Ausgabealphabete ordnen jedem Buchstaben ein gesprochenes Wort zu.
    // SpellingTokens enthält dagegen die breitere Menge akzeptierter Eingabealiase.
    public Dictionary<string, FormSpellingAlphabet> SpellingAlphabets { get; init; } =
        new(StringComparer.Ordinal);

    // Sprachbezogene Buchstabierformate wählen ein Alphabet sowie Bindewort,
    // Großschreibungsmarker und Namen technischer Zeichen.
    public Dictionary<string, FormSpokenSpellingFormatWords> SpokenSpellingFormatByLanguage { get; init; } =
        new(StringComparer.OrdinalIgnoreCase);

    // Bindewörter zwischen Buchstabe und Erläuterungswort je Sprache, etwa „wie“
    // oder „comme“. Kurze Wörter sollen nicht sprachübergreifend übersprungen werden.
    public Dictionary<string, string[]> SpellingConnectorsByLanguage { get; init; } =
        new(StringComparer.OrdinalIgnoreCase);

    // Bindewörter vor einer Diakritikbeschreibung je Sprache, etwa „mit“, „avec“ oder „con“.
    public Dictionary<string, string[]> DiacriticsConnectorTokensByLanguage { get; init; } =
        new(StringComparer.OrdinalIgnoreCase);

    // Großschreibungsmarker je Sprache, damit das Vokabular nur für die angefragte
    // Sprache oder die ersatzweise gewählte Sprache gilt.
    public Dictionary<string, string[]> UppercaseMarkersByLanguage { get; init; } =
        new(StringComparer.OrdinalIgnoreCase);

    // Modellierte Stellung des Großschreibungsmarkers je Sprache. Der Parser erlaubt
    // einen nachgestellten Marker nur bei „afterLetter“. Vorangestellte Marker prüft er immer.
    public Dictionary<string, string> UppercaseMarkerPlacementByLanguage { get; init; } =
        new(StringComparer.OrdinalIgnoreCase);

    public string[] UmlautDescriptors { get; init; } = [];

    public string[] AcuteDescriptors { get; init; } = [];

    public Dictionary<string, string> UmlautByBaseToken { get; init; } = new(StringComparer.Ordinal);

    public Dictionary<string, string> AcuteByBaseToken { get; init; } = new(StringComparer.Ordinal);

    public Dictionary<string, FormNumberWordLexicon> NumberWordsByLanguage { get; init; } =
        new(StringComparer.OrdinalIgnoreCase);

    // Bindewörter zwischen Auswahloptionen je Sprache. Kurze Wörter wie „e“ und „y“
    // können selbst Teil einer Bezeichnung sein und trennen daher nicht sprachübergreifend.
    public Dictionary<string, string[]> ChoiceConnectivesByLanguage { get; init; } =
        new(StringComparer.OrdinalIgnoreCase);

    // Füll- und Höflichkeitswörter für Auswahl-, Buchstabier- und Ziffernparser.
    // Die Verarbeitung erfolgt wortweise, daher enthält die Tabelle keine Wortgruppen.
    public Dictionary<string, string[]> FillerTokensByLanguage { get; init; } =
        new(StringComparer.OrdinalIgnoreCase);

    // Höflichkeitswörter rund um Befehle, etwa „mal“ oder „kindly“.
    // Der Index vereinigt die sprachbezogenen Daten für die Befehlsauswertung.
    public Dictionary<string, string[]> CommandPolitePaddingTokensByLanguage { get; init; } =
        new(StringComparer.OrdinalIgnoreCase);

    // Wörter für relative Tagesangaben, einleitende Partikel und Ordinalstämme.
    // Monatsnamen liefert der kulturabhängige Index.
    public Dictionary<string, FormDateInputVocabulary> DateInputVocabularyByLanguage { get; init; } =
        new(StringComparer.OrdinalIgnoreCase);

    // Sprachbezogene Schlüsselwörter nach ihrer Rolle in der Uhrzeiteingabe.
    public Dictionary<string, FormTimeInputKeywords> TimeInputKeywordsByLanguage { get; init; } =
        new(StringComparer.OrdinalIgnoreCase);

    // Sprachbezogene Grammatikdaten ergänzen die Zahlworttabellen.
    public Dictionary<string, FormNumberGrammarLexicon> NumberGrammarByLanguage { get; init; } =
        new(StringComparer.OrdinalIgnoreCase);

    // Vor der Datumsauswertung entfernte Partikel je Sprache. Deutsche
    // Anfangspartikel werden über DateInputVocabularyByLanguage behandelt.
    public Dictionary<string, string[]> DateInputParticlesByLanguage { get; init; } =
        new(StringComparer.OrdinalIgnoreCase);

    // Ordinaltage je Sprache, einschließlich englischer zusammensetzbarer Einerformen.
    // Deutsche Ordinalstämme und Endungen liegen in DateInputVocabularyByLanguage.
    public Dictionary<string, FormOrdinalDayVocabulary> OrdinalDaysByLanguage { get; init; } =
        new(StringComparer.OrdinalIgnoreCase);

    // Originalschreibweisen gesprochener Ordinaltage je Sprache und Monatstag.
    // Diese Ausgabeformen werden beim Einlesen nicht zu Vergleichsschlüsseln normalisiert.
    public Dictionary<string, Dictionary<int, string>> SpokenOrdinalDayByLanguage { get; init; } =
        new(StringComparer.OrdinalIgnoreCase);

    // Sprachbezogenes Bindewort für gesprochene Datumsformen, etwa spanisch „de“.
    public Dictionary<string, string> SpokenDateConnectorByLanguage { get; init; } =
        new(StringComparer.OrdinalIgnoreCase);

    // Sprachbezogene Wörter zur Ausgabe von Uhrzeiten. Artikel, Stundenwörter
    // und Bindewörter bleiben beim Einlesen getrennt.
    public Dictionary<string, FormSpokenTimeFormatWords> SpokenTimeFormatByLanguage { get; init; } =
        new(StringComparer.OrdinalIgnoreCase);
}
