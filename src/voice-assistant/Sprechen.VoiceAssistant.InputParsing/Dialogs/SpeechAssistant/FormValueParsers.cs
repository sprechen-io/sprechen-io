namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Erzeugt sprachbezogene Formularwertparser und bietet den direkten
/// Zugriff auf die typbezogene Rohwertinterpretation.
/// </summary>
public static class FormValueParsers
{
    public static IFormValueParser Create(
        string requestedIsoCode,
        IFormTranscriptParser transcriptParser,
        string diacriticsStrategy)
    {
        return new FormValueParser(requestedIsoCode, transcriptParser, diacriticsStrategy);
    }

    public static bool TryParseFormValue(string raw, string valueTypeKind, out FormValue value)
    {
        return FormValueParser.TryParseFormValue(raw, valueTypeKind, out value);
    }
}
