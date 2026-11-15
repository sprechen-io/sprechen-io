using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Entfernt modellierte Zustimmungsphrasen und erhält die Reihenfolge der übrigen Wörter. Die
/// Prüfung konkurrierender Hinweise kann dadurch den Rest der Antwort gesondert auswerten.
/// </summary>
internal sealed class FormsCatalogAffirmativePhraseStripper
{
    private readonly FormsCatalogIntentUtteranceResolver _utteranceResolver = new();

    public string Remove(
        FormsCatalog catalog,
        string normalized,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        var useGermanTransliteration = UsesGermanTransliteration(catalog, isoCode);
        var patterns = _utteranceResolver.GetUtterances(
                catalog,
                CatalogVoiceIntentId.ConfirmYes,
                CatalogVoiceIntentMatchMode.Phrase,
                isoCode,
                politenessLevel)
            .SelectMany(value => GetPhraseMatchingForms(value, useGermanTransliteration))
            .Select(static value => value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Where(static tokens => tokens.Length != 0)
            .OrderByDescending(static tokens => tokens.Length)
            .ToArray();
        var remainingTokens = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();

        foreach (var pattern in patterns)
        {
            RemovePattern(remainingTokens, pattern);
        }

        return string.Join(' ', remainingTokens);
    }

    private static void RemovePattern(List<string> remainingTokens, IReadOnlyList<string> pattern)
    {
        for (var start = 0; start <= remainingTokens.Count - pattern.Count;)
        {
            var matches = pattern
                .Select((token, offset) => string.Equals(
                    token,
                    remainingTokens[start + offset],
                    StringComparison.Ordinal))
                .All(static match => match);
            if (!matches)
            {
                start++;
                continue;
            }

            remainingTokens.RemoveRange(start, pattern.Count);
        }
    }

    private static IReadOnlyList<string> GetPhraseMatchingForms(
        string normalized,
        bool useGermanTransliteration)
        => useGermanTransliteration
            ? new[]
                {
                    normalized,
                    GermanUmlautTransliterator.CollapseAsciiTransliterations(normalized)
                }
                .Distinct(StringComparer.Ordinal)
                .ToArray()
            : [normalized];

    private static bool UsesGermanTransliteration(FormsCatalog catalog, string isoCode)
    {
        var promptContext = CatalogPromptContext.CreateForCatalog(catalog);
        var requestedLanguage = GetBaseLanguage(isoCode);
        var supportedLanguage = promptContext.SupportedLanguages.FirstOrDefault(
            supported => string.Equals(
                GetBaseLanguage(supported),
                requestedLanguage,
                StringComparison.OrdinalIgnoreCase));
        var effectiveLanguage = supportedLanguage is null
            ? GetBaseLanguage(promptContext.DefaultLanguage)
            : requestedLanguage;
        return string.Equals(effectiveLanguage, "de", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetBaseLanguage(string languageTag)
    {
        var separatorIndex = languageTag.IndexOf('-', StringComparison.Ordinal);
        return separatorIndex <= 0 ? languageTag : languageTag[..separatorIndex];
    }
}
