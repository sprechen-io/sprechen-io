using System.Globalization;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Localization;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Bildet die Laufzeitklassifikation von Auswahlkommandos für die Vorprüfung nach.
/// Eine direkte Abhängigkeit von Speech ist hier nicht zulässig. Deshalb müssen
/// Priorität und Vergleichsregeln beider Implementierungen übereinstimmen.
/// </summary>
internal sealed class CatalogSelectionNonNameIntentClassifier(CatalogSelectionTextNormalizer normalizer)
{
    private readonly CatalogSelectionTextNormalizer _normalizer = normalizer ?? throw new ArgumentNullException(nameof(normalizer));

    internal string? ClassifyNonNameIntent(
        FormsCatalog catalog,
        string raw,
        string language,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(raw);
        ArgumentException.ThrowIfNullOrWhiteSpace(language);

        var normalized = _normalizer.NormalizeForIntentMatching(raw, language);

        if (MatchesPhraseIntent(catalog, normalized, language, politenessLevel, CatalogVoiceIntentId.ConfirmNo))
        {
            return CatalogVoiceIntentId.ConfirmNo;
        }

        if (MatchesPhraseIntent(catalog, normalized, language, politenessLevel, CatalogVoiceIntentId.ConfirmYes))
        {
            return CatalogVoiceIntentId.ConfirmYes;
        }

        if (MatchesPhraseIntent(catalog, normalized, language, politenessLevel, CatalogVoiceIntentId.Cancel))
        {
            return CatalogVoiceIntentId.Cancel;
        }

        if (HasNonEmptyPrefixedValue(catalog, normalized, language, politenessLevel, CatalogVoiceIntentId.ChangeLanguage)
            || MatchesPhraseIntent(catalog, normalized, language, politenessLevel, CatalogVoiceIntentId.ChangeLanguage))
        {
            return CatalogVoiceIntentId.ChangeLanguage;
        }

        if (MatchesPhraseIntent(catalog, normalized, language, politenessLevel, CatalogVoiceIntentId.List))
        {
            return CatalogVoiceIntentId.List;
        }

        if (MatchesPhraseIntent(catalog, normalized, language, politenessLevel, CatalogVoiceIntentId.Help))
        {
            return CatalogVoiceIntentId.Help;
        }

        if (MatchesPhraseIntent(catalog, normalized, language, politenessLevel, CatalogVoiceIntentId.Repeat))
        {
            return CatalogVoiceIntentId.Repeat;
        }

        if (HasNonEmptyPrefixedValue(catalog, normalized, language, politenessLevel, CatalogVoiceIntentId.Search))
        {
            return CatalogVoiceIntentId.Search;
        }

        if (HasNonEmptyPrefixedValue(catalog, normalized, language, politenessLevel, CatalogVoiceIntentId.Describe))
        {
            return CatalogVoiceIntentId.Describe;
        }

        if (HasNonEmptyPrefixedValue(catalog, normalized, language, politenessLevel, CatalogVoiceIntentId.OpenCandidate))
        {
            return CatalogVoiceIntentId.OpenCandidate;
        }

        return TryExtractPositiveNumber(catalog, normalized, language, politenessLevel).HasValue
            ? CatalogVoiceIntentId.SelectByNumber
            : null;
    }

    private bool MatchesPhraseIntent(
        FormsCatalog catalog,
        string normalized,
        string language,
        PolitenessLevel politenessLevel,
        string intentId)
    {
        return GetResolvedUtterances(catalog, intentId, CatalogVoiceIntentMatchMode.Phrase, language, politenessLevel)
            .Any(utterance => CatalogSelectionTextNormalizer.MatchesAsWordOrPhrase(normalized, utterance));
    }

    private bool HasNonEmptyPrefixedValue(
        FormsCatalog catalog,
        string normalized,
        string language,
        PolitenessLevel politenessLevel,
        string intentId)
    {
        foreach (var prefix in GetResolvedUtterances(catalog, intentId, CatalogVoiceIntentMatchMode.Prefix, language, politenessLevel))
        {
            if (!CatalogSelectionTextNormalizer.StartsWithPhraseBoundary(normalized, prefix))
            {
                continue;
            }

            if (normalized[prefix.Length..].Trim().Length > 0)
            {
                return true;
            }
        }

        return false;
    }

    private int? TryExtractPositiveNumber(
        FormsCatalog catalog,
        string normalized,
        string language,
        PolitenessLevel politenessLevel)
    {
        if (int.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out var directNumber)
            && directNumber > 0)
        {
            return directNumber;
        }

        foreach (var prefix in GetResolvedUtterances(
                     catalog,
                     CatalogVoiceIntentId.SelectByNumber,
                     CatalogVoiceIntentMatchMode.Prefix,
                     language,
                     politenessLevel))
        {
            if (!CatalogSelectionTextNormalizer.StartsWithPhraseBoundary(normalized, prefix))
            {
                continue;
            }

            var remainder = normalized[prefix.Length..].Trim();
            if (int.TryParse(remainder, NumberStyles.Integer, CultureInfo.InvariantCulture, out var prefixedNumber)
                && prefixedNumber > 0)
            {
                return prefixedNumber;
            }

            var prefixedNumberWord = TryExtractNumberWordValue(catalog, remainder, language, politenessLevel);
            if (prefixedNumberWord.HasValue)
            {
                return prefixedNumberWord;
            }
        }

        return TryExtractNumberWordValue(catalog, normalized, language, politenessLevel);
    }

    private int? TryExtractNumberWordValue(
        FormsCatalog catalog,
        string normalized,
        string language,
        PolitenessLevel politenessLevel)
    {
        foreach (var numberWord in catalog.SelectionDialog.NumberWords)
        {
            foreach (var utterance in ResolveLocalizedTexts(catalog, numberWord.Utterances, language, politenessLevel))
            {
                if (CatalogSelectionTextNormalizer.MatchesAsWordOrPhrase(normalized, utterance))
                {
                    return numberWord.Value;
                }
            }
        }

        return null;
    }

    private IReadOnlyList<string> GetResolvedUtterances(
        FormsCatalog catalog,
        string intentId,
        string matchMode,
        string language,
        PolitenessLevel politenessLevel)
    {
        var resolved = new List<string>();
        foreach (var intent in catalog.SelectionDialog.Intents.Where(item =>
                     string.Equals(item.IntentId, intentId, StringComparison.Ordinal)
                     && string.Equals(item.MatchMode, matchMode, StringComparison.Ordinal)))
        {
            resolved.AddRange(ResolveLocalizedTexts(catalog, intent.Utterances, language, politenessLevel));
        }

        return resolved.Distinct(StringComparer.Ordinal).ToArray();
    }

    private IReadOnlyList<string> ResolveLocalizedTexts(
        FormsCatalog catalog,
        IReadOnlyList<LocalizedText> texts,
        string language,
        PolitenessLevel politenessLevel)
    {
        var supportedLanguages = catalog.SelectionDialog.SupportedLanguages
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (supportedLanguages.Length == 0)
        {
            supportedLanguages = catalog.Applications
                .SelectMany(application => application.SupportedLanguages)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        if (supportedLanguages.Length == 0)
        {
            supportedLanguages = ["en"];
        }

        var defaultLanguage = !string.IsNullOrWhiteSpace(catalog.SelectionDialog.DefaultLanguage)
            && supportedLanguages.Contains(catalog.SelectionDialog.DefaultLanguage, StringComparer.OrdinalIgnoreCase)
                ? catalog.SelectionDialog.DefaultLanguage
                : supportedLanguages[0];

        var resolved = new List<string>(texts.Count);
        foreach (var text in texts)
        {
            if (LocalizedTextResolver.TryResolve(
                    text,
                    language,
                    defaultLanguage,
                    supportedLanguages,
                    strictLocalization: false,
                    politenessLevel,
                    out var localized)
                && !string.IsNullOrWhiteSpace(localized))
            {
                resolved.Add(_normalizer.NormalizeForIntentMatching(localized, language));
            }
        }

        return resolved.Distinct(StringComparer.Ordinal).ToArray();
    }
}
