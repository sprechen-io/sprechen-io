using System.Globalization;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Vergleicht normalisierte Eingaben mit modellierten Katalogphrasen, Präfixen und Zahlwörtern.
/// Die einzelnen Intent-Klassifizierer nutzen dadurch dieselben sprachabhängigen Vergleichsregeln.
/// </summary>
internal sealed class FormsCatalogIntentMatcher : IFormsCatalogIntentMatcher
{
    private readonly FormsCatalogIntentUtteranceResolver _utteranceResolver = new();

    public bool IsConfirmNo(
        FormsCatalog catalog,
        string normalized,
        string isoCode,
        PolitenessLevel politenessLevel)
        => MatchesPhraseIntent(catalog, normalized, isoCode, politenessLevel, CatalogVoiceIntentId.ConfirmNo);

    public bool IsConfirmYes(
        FormsCatalog catalog,
        string normalized,
        string isoCode,
        PolitenessLevel politenessLevel)
        => MatchesPhraseIntent(catalog, normalized, isoCode, politenessLevel, CatalogVoiceIntentId.ConfirmYes);

    public bool IsCancel(
        FormsCatalog catalog,
        string normalized,
        string isoCode,
        PolitenessLevel politenessLevel)
        => MatchesPhraseIntent(catalog, normalized, isoCode, politenessLevel, CatalogVoiceIntentId.Cancel);

    public bool IsChangeLanguage(
        FormsCatalog catalog,
        string normalized,
        string isoCode,
        PolitenessLevel politenessLevel)
        => MatchesPhraseIntent(catalog, normalized, isoCode, politenessLevel, CatalogVoiceIntentId.ChangeLanguage);

    public string? TryExtractChangeLanguageTarget(
        FormsCatalog catalog,
        string normalized,
        string isoCode,
        PolitenessLevel politenessLevel)
        => TryExtractPrefixedValue(
            catalog,
            normalized,
            isoCode,
            politenessLevel,
            CatalogVoiceIntentId.ChangeLanguage);

    public bool IsRepeat(
        FormsCatalog catalog,
        string normalized,
        string isoCode,
        PolitenessLevel politenessLevel)
        => MatchesPhraseIntent(catalog, normalized, isoCode, politenessLevel, CatalogVoiceIntentId.Repeat);

    public bool IsHelp(
        FormsCatalog catalog,
        string normalized,
        string isoCode,
        PolitenessLevel politenessLevel)
        => MatchesPhraseIntent(catalog, normalized, isoCode, politenessLevel, CatalogVoiceIntentId.Help);

    public bool IsList(
        FormsCatalog catalog,
        string normalized,
        string isoCode,
        PolitenessLevel politenessLevel)
        => MatchesPhraseIntent(catalog, normalized, isoCode, politenessLevel, CatalogVoiceIntentId.List);

    public string? TryExtractSearchQuery(
        FormsCatalog catalog,
        string normalized,
        string isoCode,
        PolitenessLevel politenessLevel)
        => TryExtractPrefixedValue(catalog, normalized, isoCode, politenessLevel, CatalogVoiceIntentId.Search);

    public string? TryExtractDescribeTarget(
        FormsCatalog catalog,
        string normalized,
        string isoCode,
        PolitenessLevel politenessLevel)
        => TryExtractPrefixedValue(catalog, normalized, isoCode, politenessLevel, CatalogVoiceIntentId.Describe);

    public string? TryExtractOpenCandidateTarget(
        FormsCatalog catalog,
        string normalized,
        string isoCode,
        PolitenessLevel politenessLevel)
        => TryExtractPrefixedValue(
            catalog,
            normalized,
            isoCode,
            politenessLevel,
            CatalogVoiceIntentId.OpenCandidate);

    public int? TryExtractNumber(
        FormsCatalog catalog,
        string normalized,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        if (int.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out var directNumber) && directNumber > 0)
        {
            return directNumber;
        }

        var prefixedNumber = TryExtractNumberFromNumberPrefix(
            catalog,
            normalized,
            isoCode,
            politenessLevel);
        if (prefixedNumber.HasValue)
        {
            return prefixedNumber.Value;
        }

        return TryExtractNumberWordValue(catalog, normalized, isoCode, politenessLevel);
    }

    private bool MatchesPhraseIntent(
        FormsCatalog catalog,
        string normalized,
        string isoCode,
        PolitenessLevel politenessLevel,
        string intentId)
    {
        var useGermanTransliteration = UsesGermanTransliteration(catalog, isoCode);
        var inputForms = GetPhraseMatchingForms(normalized, useGermanTransliteration);
        return _utteranceResolver.GetUtterances(
                catalog,
                intentId,
                CatalogVoiceIntentMatchMode.Phrase,
                isoCode,
                politenessLevel)
            .Any(utterance => GetPhraseMatchingForms(utterance, useGermanTransliteration)
                .Any(pattern => inputForms.Any(input => TextNormalizer.MatchesAsWordOrPhrase(input, pattern))));
    }

    private string? TryExtractPrefixedValue(
        FormsCatalog catalog,
        string normalized,
        string isoCode,
        PolitenessLevel politenessLevel,
        string intentId)
    {
        var useGermanTransliteration = UsesGermanTransliteration(catalog, isoCode);
        var inputForms = GetPhraseMatchingForms(normalized, useGermanTransliteration);
        var prefixes = _utteranceResolver.GetUtterances(
            catalog,
            intentId,
            CatalogVoiceIntentMatchMode.Prefix,
            isoCode,
            politenessLevel);

        foreach (var input in inputForms)
        {
            foreach (var prefix in prefixes.SelectMany(
                         value => GetPhraseMatchingForms(value, useGermanTransliteration)))
            {
                if (!StartsWithPhraseBoundary(input, prefix))
                {
                    continue;
                }

                var remainder = input[prefix.Length..].Trim();
                if (!string.IsNullOrWhiteSpace(remainder))
                {
                    return remainder;
                }
            }
        }

        return null;
    }

    private int? TryExtractNumberWordValue(
        FormsCatalog catalog,
        string normalized,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        var promptContext = CatalogPromptContext.CreateForCatalog(catalog);
        var useGermanTransliteration = UsesGermanTransliteration(promptContext, isoCode);
        var inputForms = GetPhraseMatchingForms(normalized, useGermanTransliteration);
        var numberWords = catalog.SelectionDialog.NumberWords;
        if (numberWords is null)
        {
            return null;
        }

        foreach (var numberWord in numberWords)
        {
            foreach (var utterance in _utteranceResolver.ResolveLocalizedTexts(
                         numberWord.Utterances,
                         promptContext,
                         isoCode,
                         politenessLevel))
            {
                if (GetPhraseMatchingForms(utterance, useGermanTransliteration)
                    .Any(pattern => inputForms.Any(input => TextNormalizer.MatchesAsWordOrPhrase(input, pattern))))
                {
                    return numberWord.Value;
                }
            }
        }

        return null;
    }

    private int? TryExtractNumberFromNumberPrefix(
        FormsCatalog catalog,
        string normalized,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        var remainder = TryExtractPrefixedValue(
            catalog,
            normalized,
            isoCode,
            politenessLevel,
            CatalogVoiceIntentId.SelectByNumber);
        if (string.IsNullOrWhiteSpace(remainder))
        {
            return null;
        }

        if (int.TryParse(remainder, NumberStyles.Integer, CultureInfo.InvariantCulture, out var directNumber) && directNumber > 0)
        {
            return directNumber;
        }

        return TryExtractNumberWordValue(catalog, remainder, isoCode, politenessLevel);
    }

    private static IReadOnlyList<string> GetPhraseMatchingForms(
        string normalized,
        bool useGermanTransliteration)
    {
        if (!useGermanTransliteration)
        {
            return [normalized];
        }

        return new[]
        {
            normalized,
            GermanUmlautTransliterator.CollapseAsciiTransliterations(normalized)
        }
        .Distinct(StringComparer.Ordinal)
        .ToArray();
    }

    private static bool UsesGermanTransliteration(FormsCatalog catalog, string isoCode)
        => UsesGermanTransliteration(CatalogPromptContext.CreateForCatalog(catalog), isoCode);

    private static bool UsesGermanTransliteration(CatalogPromptContext promptContext, string isoCode)
    {
        var requestedLanguage = GetBaseLanguage(isoCode);
        if (promptContext.SupportedLanguages.Any(
                supported => string.Equals(
                    GetBaseLanguage(supported),
                    requestedLanguage,
                    StringComparison.OrdinalIgnoreCase)))
        {
            return string.Equals(requestedLanguage, "de", StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(
            GetBaseLanguage(promptContext.DefaultLanguage),
            "de",
            StringComparison.OrdinalIgnoreCase);
    }

    private static string GetBaseLanguage(string languageTag)
    {
        var separatorIndex = languageTag.IndexOf('-', StringComparison.Ordinal);
        return separatorIndex <= 0 ? languageTag : languageTag[..separatorIndex];
    }

    private static bool StartsWithPhraseBoundary(string input, string prefix)
        => input.StartsWith(prefix, StringComparison.Ordinal)
           && (input.Length == prefix.Length || input[prefix.Length] == ' ');
}
