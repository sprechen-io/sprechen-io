using System.Text.Json;
using Sprechen.Mda.Contracts.V1.RuntimeArtifacts;

namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Liest den Wortschatz für gesprochene Datums- und Zeitwerte aus dem eingebetteten
/// Formular-Transkript-Lexikon der gemeinsamen MDA-Vertragsassembly. Tagesformen,
/// Datumsverbinder, Zeitformatwörter und Jahresgrammatik werden gemeinsam geladen,
/// damit die Ausgabe den modellierten Wortlaut verwendet.
/// </summary>
internal static class FormTemporalOutputVocabulary
{
    private const string LexiconPath =
        "dialogmodell/reference-contracts/voice-interface/form-transcript-lexicon.reference.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly Lazy<FormTemporalOutputLexicon> SharedLexicon = new(LoadLexicon);

    private static readonly IReadOnlyDictionary<int, string> EmptyOrdinalDays =
        new Dictionary<int, string>();

    /// <summary>
    /// Gesprochene Tagesformen für den Sprachcode, zugeordnet nach Tag im Monat.
    /// Für Englisch, Spanisch und Italienisch bleibt die Zuordnung leer. Dort werden Ziffern verwendet.
    /// </summary>
    internal static IReadOnlyDictionary<int, string> SpokenOrdinalDays(string isoCode)
        => SharedLexicon.Value.SpokenOrdinalDayByLanguage.TryGetValue(isoCode, out var ordinalDays)
            ? ordinalDays
            : EmptyOrdinalDays;

    /// <summary>
    /// Verbindungswort im Datum, etwa de auf Spanisch. Fehlt eine Verbindung zwischen
    /// Tag, Monat und Jahr, wird null zurückgegeben.
    /// </summary>
    internal static string? SpokenDateConnector(string isoCode)
        => SharedLexicon.Value.SpokenDateConnectorByLanguage.TryGetValue(isoCode, out var connector)
            ? connector
            : null;

    /// <summary>
    /// Wörter für die einzelnen Bestandteile einer gesprochenen Uhrzeit.
    /// Für nicht unterstützte Sprachen bleiben sie leer. Die Ausgabe behält dann HH:mm.
    /// </summary>
    internal static FormSpokenTimeFormat SpokenTimeFormatWords(string isoCode)
        => SharedLexicon.Value.SpokenTimeFormatByLanguage.TryGetValue(isoCode, out var formatWords)
            ? formatWords
            : FormSpokenTimeFormat.Empty;

    /// <summary>
    /// Wörter für ausgeschriebene Jahreszahlen auf Deutsch und Englisch.
    /// Bei anderen Sprachen bleiben die Rollen leer und die Jahreszahl wird in Ziffern ausgegeben.
    /// </summary>
    internal static FormSpokenYearNumberGrammar SpokenYearNumberGrammar(string isoCode)
        => SharedLexicon.Value.NumberGrammarByLanguage.TryGetValue(isoCode, out var grammar)
            ? grammar
            : FormSpokenYearNumberGrammar.Empty;

    private static FormTemporalOutputLexicon LoadLexicon()
    {
        var json = MdaEmbeddedRuntimeArtifacts.ReadAllBytes(LexiconPath);
        return JsonSerializer.Deserialize<FormTemporalOutputLexicon>(json, SerializerOptions)
            ?? throw new InvalidOperationException($"Embedded transcript lexicon '{LexiconPath}' could not be deserialized.");
    }
}
