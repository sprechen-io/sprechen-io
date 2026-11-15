using System.Reflection;
using System.Text.Json;

namespace Sprechen.Service.Deepgram.Services;

/// <summary>
/// Liest die Schlüssel von spellingTokens aus der eingebetteten Lexikonressource
/// als Erkennungshinweise für Buchstabierwörter. Fehlende Einträge und ungültiges JSON liefern eine leere Liste.
/// </summary>
internal sealed class SpellingKeytermVocabulary
{
    private const string CanonicalResourceName =
        "Sprechen.Service.Deepgram.Resources.form-transcript-lexicon.default.json";

    private const string SpellingTokensPropertyName = "spellingTokens";

    private static readonly SpellingKeytermVocabulary DefaultInstance =
        new(ReadCanonicalKeyterms());

    internal SpellingKeytermVocabulary(IEnumerable<string> keyterms)
    {
        ArgumentNullException.ThrowIfNull(keyterms);

        var distinct = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var keyterm in keyterms)
        {
            if (!string.IsNullOrEmpty(keyterm) && seen.Add(keyterm))
            {
                distinct.Add(keyterm);
            }
        }

        Keyterms = distinct;
    }

    internal static SpellingKeytermVocabulary Default => DefaultInstance;

    /// <summary>
    /// Eindeutige, nicht leere Buchstabierwörter in der Reihenfolge der Eingabe.
    /// </summary>
    internal IReadOnlyList<string> Keyterms { get; }

    private static IReadOnlyList<string> ReadCanonicalKeyterms()
    {
        try
        {
            var assembly = typeof(SpellingKeytermVocabulary).Assembly;
            using var stream = assembly.GetManifestResourceStream(CanonicalResourceName);
            if (stream is null)
            {
                return [];
            }

            using var document = JsonDocument.Parse(stream);
            if (!document.RootElement.TryGetProperty(SpellingTokensPropertyName, out var spellingTokens)
                || spellingTokens.ValueKind != JsonValueKind.Object)
            {
                return [];
            }

            var keyterms = new List<string>();
            foreach (var token in spellingTokens.EnumerateObject())
            {
                keyterms.Add(token.Name);
            }

            return keyterms;
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
