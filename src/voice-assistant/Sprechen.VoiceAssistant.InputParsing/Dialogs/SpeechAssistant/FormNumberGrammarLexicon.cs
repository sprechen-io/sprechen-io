namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Enthält sprachbezogene Grammatik- und Formatwörter rund um Zahlwörter.
/// Die Daten ergänzen die Werttabellen. Die Zusammensetzung übernehmen die Parser.
/// </summary>
internal sealed class FormNumberGrammarLexicon
{
    // Bindewort innerhalb zusammengesetzter Zahlen, etwa „und“ in „dreiundzwanzig“.
    public string? AndConnector { get; init; }

    // Größenwörter ohne unterstützte Zusammensetzung oder Zehnerpotenz, etwa
    // „dutzend“. Der deutsche Zahlwortparser meldet sie als zu groß.
    public string[] UnsupportedMagnitudeWords { get; init; } = [];

    // Gesprochene negative Vorzeichen, etwa „minus“, „negativ“ oder „negative“.
    public string[] SignWords { get; init; } = [];

    // Gebundene Einerform für deutsche Zusammensetzungen, etwa „ein“ in „einhundert“.
    public string? BoundOnePrefix { get; init; }

    // Englische Nullform für Jahreszahlen wie „nineteen oh five“.
    public string? OhWord { get; init; }

    // Gesprochenes Hunderterwort, etwa „hundert“ oder „hundred“.
    public string? HundredWord { get; init; }

    // Gesprochenes Tausenderwort, etwa „tausend“ oder „thousand“.
    public string? ThousandWord { get; init; }
}
