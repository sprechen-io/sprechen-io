namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Enthält sprachbezogene Ausgabevorgaben für phonetisches Buchstabieren,
/// damit Alphabet, Marker, Bindewörter und Zeichennamen gemeinsam auflösbar sind.
/// </summary>
internal sealed class FormSpokenSpellingFormatWords
{
    public string Alphabet { get; init; } = string.Empty;

    public string Connector { get; init; } = string.Empty;

    public string UppercaseMarker { get; init; } = string.Empty;

    public Dictionary<string, string> CharacterNames { get; init; } = new(StringComparer.Ordinal);

    // Optionale Ziffernwörter je Sprache für die gesprochene Buchstabierausgabe.
    // Mit Eintrag kann der Renderer etwa „7 wie sieben“ ausgeben.
    // Ohne Eintrag bleibt die Ziffer ein einzelnes Zeichen.
    public Dictionary<string, string> DigitWords { get; init; } = new(StringComparer.Ordinal);
}
