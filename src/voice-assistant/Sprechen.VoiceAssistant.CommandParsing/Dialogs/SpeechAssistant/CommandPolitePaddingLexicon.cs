using System.Text.Json;

namespace Sprechen.VoiceAssistant.CommandParsing.Dialogs.SpeechAssistant;

/// <summary>
/// Stellt Befehlsparsern die modellierten Höflichkeitswörter als gemeinsame Nachschlagetabelle bereit.
/// Die sprachweise gespeicherten Wörter werden sprachübergreifend vereinigt und wie die
/// verglichenen Transkriptwörter normalisiert. Die Ressource wird beim Build eingebettet.
/// Fehlt sie oder ist sie fehlerhaft, bricht das Laden ab.
/// </summary>
internal static class CommandPolitePaddingLexicon
{
    private const string CanonicalResourceName =
        "Sprechen.VoiceAssistant.CommandParsing.Resources.form-transcript-lexicon.default.json";

    private const string SectionPropertyName = "commandPolitePaddingTokensByLanguage";

    private static readonly Lazy<IReadOnlySet<string>> SharedUnionTokens = new(LoadUnionTokens);

    // Höflichkeitswörter dürfen einen Befehl unabhängig von der Sitzungssprache einrahmen.
    internal static IReadOnlySet<string> UnionTokens => SharedUnionTokens.Value;

    private static IReadOnlySet<string> LoadUnionTokens()
    {
        var assembly = typeof(CommandPolitePaddingLexicon).Assembly;
        using var stream = assembly.GetManifestResourceStream(CanonicalResourceName)
            ?? throw new InvalidOperationException($"Missing embedded transcript lexicon resource '{CanonicalResourceName}'.");
        using var document = JsonDocument.Parse(stream);
        if (!document.RootElement.TryGetProperty(SectionPropertyName, out var section)
            || section.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException(
                $"The embedded transcript lexicon carries no '{SectionPropertyName}' section.");
        }

        var union = new HashSet<string>(StringComparer.Ordinal);
        foreach (var languageEntry in section.EnumerateObject())
        {
            if (languageEntry.Value.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException(
                    $"The '{SectionPropertyName}' entry '{languageEntry.Name}' is not a token array.");
            }

            foreach (var token in languageEntry.Value.EnumerateArray())
            {
                var normalizedToken = TextNormalizer.NormalizeForPhraseMatching(token.GetString() ?? string.Empty);
                if (normalizedToken.Length != 0)
                {
                    union.Add(normalizedToken);
                }
            }
        }

        return union;
    }
}
