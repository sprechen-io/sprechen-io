using System.Collections.Immutable;

namespace Sprechen.Speech.LanguageContracts;

/// <summary>
/// Bereitet Sprachwechselargumente durch Normalisierung und Entfernen bekannter Randwörter für den Zielvergleich vor.
/// </summary>
internal sealed class LanguageChangeCandidateNormalizer
{
    /// <summary>
    /// Enthält bekannte nachgestellte Begleitwörter, die den Sprachwunsch unverändert lassen.
    /// </summary>
    /// <remarks>
    /// Die Liste ergänzt die vorderen Begleitwörter, damit etwa „italiano per favore“
    /// auf denselben Sprachkandidaten wie „italiano“ reduziert werden kann.
    /// </remarks>
    private static readonly ImmutableHashSet<string> TrailingNoiseTokens =
        ImmutableHashSet.Create(
            StringComparer.Ordinal,
            "bitte",
            "danke",
            "favor",
            "favore",
            "favore",
            "grazie",
            "gracias",
            "merci",
            "please",
            "por",
            "per",
            "prego",
            // Auch nachgestellter englischer Dank wie English thanks oder English thank you wird entfernt.
            "thanks",
            "thank",
            "you",
            "molte",
            "muchas",
            "mille",
            // Die Normalisierung zerlegt s'il in s und il. Deshalb sind die Teile einzeln enthalten.
            "s",
            "s'il",
            "sil",
            "il",
            "vous",
            "plait",
            "plaît");

    private static readonly ImmutableHashSet<string> LeadingNoiseTokens =
        ImmutableHashSet.Create(
            StringComparer.Ordinal,
            "a",
            "al",
            "au",
            "auf",
            "bitte",
            "das",
            "de",
            "der",
            "die",
            "el",
            "en",
            "favor",
            "idioma",
            "il",
            "in",
            "into",
            "la",
            "langue",
            "language",
            "le",
            "lingua",
            "lo",
            "nach",
            "para",
            "per",
            "please",
            "por",
            "sil",
            "sprache",
            // Verben und Partikel erlauben Bitten wie Parla italiano oder Vorrei l'italiano.
            // Nur Randwörter entfernen: Ein Sprachname innerhalb eines Kurstitels soll nicht genügen.
            // Negationen wie kein bleiben stehen, sodass kein italienisch kein Sprachziel ergibt.
            // Elisionsreste wie l aus l'italiano müssen nach der Normalisierung einzeln entfallen.
            "d",
            "dell",
            "della",
            "l",
            "gerne",
            "haette",
            "hatte",
            "ich",
            "io",
            "je",
            "moechte",
            "mochte",
            "parla",
            "parlo",
            "quiero",
            "quisiera",
            "speak",
            "su",
            "sur",
            "to",
            "vers",
            "voglio",
            "vorrei",
            "voudrais",
            "vous",
            "want",
            "zu");

    private readonly ILanguageCatalog _languageCatalog;

    public LanguageChangeCandidateNormalizer(ILanguageCatalog languageCatalog)
    {
        _languageCatalog = languageCatalog ?? throw new ArgumentNullException(nameof(languageCatalog));
    }

    public string Normalize(string? rawArgument)
    {
        if (string.IsNullOrWhiteSpace(rawArgument))
        {
            return string.Empty;
        }

        if (LanguageCode.TryParseIso6391(rawArgument, out var language)
            && _languageCatalog.IsSupported(language))
        {
            return language.Value;
        }

        var normalized = TextNormalizer.NormalizeForPhraseMatching(rawArgument);
        return normalized.Length == 0
            ? string.Empty
            : TrimNoise(normalized);
    }

    /// <summary>
    /// Entfernt bekannte Begleitwörter am Anfang und Ende des normalisierten Kandidaten.
    /// </summary>
    /// <remarks>
    /// Wörter im Inneren bleiben erhalten. Nur zusammenhängende bekannte Randwörter entfallen.
    /// </remarks>
    private static string TrimNoise(string normalized)
    {
        var tokens = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var start = 0;
        while (start < tokens.Length && LeadingNoiseTokens.Contains(tokens[start]))
        {
            start++;
        }

        var end = tokens.Length;
        while (end > start && TrailingNoiseTokens.Contains(tokens[end - 1]))
        {
            end--;
        }

        tokens = tokens[..end];

        return start >= tokens.Length
            ? string.Empty
            : string.Join(' ', tokens[start..]);
    }
}
