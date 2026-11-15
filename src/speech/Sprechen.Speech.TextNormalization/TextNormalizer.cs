namespace Sprechen.Speech.TextNormalization;

/// <summary>
/// Bietet Aufrufern passende Vergleichsformen für Wörter, Wortfolgen und kompakte Werte. Ein eigener akzenterhaltender Weg schützt bedeutungsunterscheidende Akzente.
/// </summary>
public static class TextNormalizer
{
    /// <summary>
    /// Normalisiert Text für Vergleiche einzelner Wörter und erhält dabei
    /// Trennzeichen wie Satzzeichen und innere Leerzeichen.
    /// </summary>
    /// <remarks>
    /// Kulturunabhängige Kleinschreibung, Entfernen äußerer Leerzeichen und
    /// Diakritika, etwa ä zu a oder é zu e. Keine Umschrift von Umlauten in ae, oe oder ue.
    /// </remarks>
    public static string NormalizeForTokenComparison(string raw)
    {
        return TextDiacriticsNormalizer.Normalize(raw);
    }

    /// <summary>
    /// Normalisiert Text für Wortfolgenvergleiche und erhält die Wortgrenzen.
    /// </summary>
    /// <remarks>
    /// Kleinschreibung und Entfernen von Diakritika. Andere Zeichen als Buchstaben
    /// und Ziffern werden zu Leerzeichen. Mehrere Abstände werden zusammengefasst
    /// und äußere Leerzeichen entfernt.
    /// </remarks>
    public static string NormalizeForPhraseMatching(string raw)
    {
        return PhraseTextNormalizer.Normalize(raw);
    }

    /// <summary>
    /// Normalisiert Wortfolgen für Vergleiche, bei denen Akzente die
    /// Bedeutungsunterscheidung tragen, etwa bei spanischem „sí“ und italienischem „sì“.
    /// </summary>
    /// <remarks>
    /// Kulturunabhängige Kleinschreibung und kanonische Zusammensetzung erhalten
    /// Akzente, die Bestandteil zusammengesetzter Buchstaben sind. Andere Zeichen
    /// als Buchstaben und Ziffern werden zu Wortabständen. Diese Variante setzt
    /// akzentgetreue Erkennung voraus. Für akzentunabhängige Vergleiche dient
    /// <see cref="NormalizeForPhraseMatching"/>.
    /// </remarks>
    public static string NormalizeForAccentSensitiveMatching(string raw)
    {
        return AccentSensitivePhraseTextNormalizer.Normalize(raw);
    }

    /// <summary>
    /// Normalisiert Text für kompakte Vergleiche ohne Wortabstände.
    /// </summary>
    /// <remarks>
    /// Kleinschreibung und Entfernen von Diakritika. Ausschließlich Buchstaben
    /// und Ziffern bleiben erhalten. Das Ergebnis wird kanonisch zusammengesetzt.
    /// </remarks>
    public static string NormalizeForCompactMatching(string raw)
    {
        return CompactTextNormalizer.Normalize(raw);
    }

    /// <summary>
    /// Prüft, ob ein Muster an Wort- oder Textgrenzen im bereits normalisierten Text vorkommt.
    /// </summary>
    public static bool MatchesAsWordOrPhrase(string normalizedText, string normalizedPattern)
    {
        return NormalizedWordPhraseMatcher.Matches(normalizedText, normalizedPattern);
    }

    /// <summary>
    /// Zerlegt bereits normalisierten Text in zusammenhängende Folgen ausschließlich aus Buchstaben.
    /// </summary>
    public static IEnumerable<string> TokenizeLetters(string normalizedText)
    {
        return NormalizedTextTokenizer.TokenizeLetters(normalizedText);
    }

    /// <summary>
    /// Zerlegt bereits normalisierten Text in zusammenhängende Folgen aus Buchstaben oder Ziffern.
    /// </summary>
    public static IEnumerable<string> TokenizeLettersOrDigits(string normalizedText)
    {
        return NormalizedTextTokenizer.TokenizeLettersOrDigits(normalizedText);
    }
}

