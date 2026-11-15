namespace Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

/// <summary>
/// Deserialisiert die Ausgabeabschnitte spokenOrdinalDayByLanguage,
/// spokenDateConnectorByLanguage, spokenTimeFormatByLanguage und numberGrammarByLanguage
/// des Formular-Transkript-Lexikons, damit die Dialoglaufzeit den modellierten Wortschatz verwendet.
/// </summary>
internal sealed class FormTemporalOutputLexicon
{
    public Dictionary<string, Dictionary<int, string>> SpokenOrdinalDayByLanguage { get; init; } =
        new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, string> SpokenDateConnectorByLanguage { get; init; } =
        new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, FormSpokenTimeFormat> SpokenTimeFormatByLanguage { get; init; } =
        new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, FormSpokenYearNumberGrammar> NumberGrammarByLanguage { get; init; } =
        new(StringComparer.OrdinalIgnoreCase);
}
