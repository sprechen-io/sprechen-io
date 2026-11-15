namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Trennt Mehrfachauswahltranskripte vor der Normalisierung in Optionssegmente, damit
/// Satzzeichengrenzen erhalten bleiben. Übergebene Bindewörter trennen nur ganze, durch
/// Leerzeichen begrenzte Wörter: „E-Mail“ wird nicht durch das italienische „e“ geteilt.
/// Das sprachabhängige Vokabular liefert der Aufrufer.
/// </summary>
internal static class FormChoiceTranscriptSplitter
{
    // „|“ trennt auch die kanonische Mengenform „email|sms“, damit bereits gesammelte
    // Codes über denselben Transkriptpfad erneut eingelesen werden können.
    private static readonly char[] PunctuationSeparators = [',', ';', '|'];

    public static IReadOnlyList<string> SplitRaw(string rawTranscript, IReadOnlySet<string> normalizedConnectives)
    {
        ArgumentNullException.ThrowIfNull(rawTranscript);
        ArgumentNullException.ThrowIfNull(normalizedConnectives);

        var segments = new List<string>();
        foreach (var piece in rawTranscript.Split(PunctuationSeparators, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            SplitOnConnectiveWords(piece, normalizedConnectives, segments);
        }

        return segments;
    }

    private static void SplitOnConnectiveWords(string piece, IReadOnlySet<string> normalizedConnectives, List<string> segments)
    {
        var currentSegmentTokens = new List<string>();
        foreach (var token in piece.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            // Ein normalisiertes Wort vergleichen: „Und“ passt. „E-Mail“ wird zu „e mail“
            // und passt daher nicht zum einzelnen Bindewort „e“.
            if (normalizedConnectives.Contains(TextNormalizer.NormalizeForPhraseMatching(token)))
            {
                FlushSegment(currentSegmentTokens, segments);
                continue;
            }

            currentSegmentTokens.Add(token);
        }

        FlushSegment(currentSegmentTokens, segments);
    }

    private static void FlushSegment(List<string> currentSegmentTokens, List<string> segments)
    {
        if (currentSegmentTokens.Count > 0)
        {
            segments.Add(string.Join(' ', currentSegmentTokens));
        }

        currentSegmentTokens.Clear();
    }
}
