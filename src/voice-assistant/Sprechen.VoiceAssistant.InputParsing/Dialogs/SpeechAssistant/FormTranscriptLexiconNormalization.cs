namespace Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Leitet normalisierte Vergleichsformen für Parser aus den Modellschreibweisen ab.
/// Die Originalschreibweisen bleiben für Erkennungshinweise verfügbar, weil verlorene
/// Diakritika nicht zuverlässig aus den Vergleichsschlüsseln rekonstruiert werden können.
/// </summary>
internal static class FormTranscriptLexiconNormalization
{
    /// <summary>
    /// Normalisiert nur die Schlüssel einer Zuordnung. Werte wie Umlautzeichen bleiben
    /// erhalten. Kollidierende Schlüssel mit verschiedenen Werten führen zum Fehler,
    /// statt still einen Wert zu überschreiben. Leere Vergleichsschlüssel entfallen.
    /// </summary>
    internal static Dictionary<string, TValue> NormalizeKeys<TValue>(IReadOnlyDictionary<string, TValue> source)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(source);

        var normalized = new Dictionary<string, TValue>(StringComparer.Ordinal);
        foreach (var (key, value) in source)
        {
            var normalizedKey = TextNormalizer.NormalizeForPhraseMatching(key);
            if (normalizedKey.Length == 0)
            {
                continue;
            }

            if (normalized.TryGetValue(normalizedKey, out var existing) && !existing.Equals(value))
            {
                throw new InvalidOperationException(
                    $"Lexicon key '{key}' normalizes to '{normalizedKey}', which another entry already maps to a different value.");
            }

            normalized[normalizedKey] = value;
        }

        return normalized;
    }

    /// <summary>
    /// Bildet eine Menge normalisierter Wörter für den Vergleich mit
    /// normalisierten Transkripttokens. Leere Vergleichsformen entfallen.
    /// </summary>
    internal static HashSet<string> NormalizeWordTokens(IEnumerable<string> tokens)
    {
        ArgumentNullException.ThrowIfNull(tokens);

        var normalized = new HashSet<string>(StringComparer.Ordinal);
        foreach (var token in tokens)
        {
            var normalizedToken = TextNormalizer.NormalizeForPhraseMatching(token);
            if (normalizedToken.Length != 0)
            {
                normalized.Add(normalizedToken);
            }
        }

        return normalized;
    }

    internal static FormNumberWordLexicon NormalizeNumberWords(FormNumberWordLexicon source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new FormNumberWordLexicon
        {
            Units = NormalizeKeys(source.Units),
            Teens = NormalizeKeys(source.Teens),
            Tens = NormalizeKeys(source.Tens),
            Scales = NormalizeKeys(source.Scales),
        };
    }

    internal static FormDateInputVocabulary NormalizeDateInputVocabulary(FormDateInputVocabulary source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new FormDateInputVocabulary
        {
            RelativeDayOffsets = NormalizeKeys(source.RelativeDayOffsets),

            // Partikel und Endungen sind bereits in Vergleichsform hinterlegt
            // und werden unverändert für das Entfernen aus dem Transkript übernommen.
            LeadingParticles = source.LeadingParticles,
            OrdinalEndings = source.OrdinalEndings,
            OrdinalDayStems = NormalizeKeys(source.OrdinalDayStems),
        };
    }
}
