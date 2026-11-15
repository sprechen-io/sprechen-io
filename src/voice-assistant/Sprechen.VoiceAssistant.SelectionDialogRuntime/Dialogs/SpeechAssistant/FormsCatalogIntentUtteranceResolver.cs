using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Localization;

namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Löst modellierte Befehlsäußerungen nach Sprache und Anrede auf und normalisiert sie
/// für den Phrasenvergleich. Parser und Erkennungshinweise können so dieselbe Katalogquelle nutzen.
/// </summary>
internal sealed class FormsCatalogIntentUtteranceResolver
{
    internal IReadOnlyList<string> GetUtterances(
        FormsCatalog catalog,
        string intentId,
        string matchMode,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        var promptContext = CatalogPromptContext.CreateForCatalog(catalog);
        var intents = catalog.SelectionDialog.Intents?
            .Where(item =>
                string.Equals(item.IntentId, intentId, StringComparison.Ordinal)
                && string.Equals(item.MatchMode, matchMode, StringComparison.Ordinal))
            .ToArray();
        if (intents is null || intents.Length == 0)
        {
            return [];
        }

        var resolved = new List<string>();
        foreach (var intent in intents)
        {
            resolved.AddRange(ResolveLocalizedTexts(
                intent.Utterances,
                promptContext,
                isoCode,
                politenessLevel));
        }

        return resolved.Distinct(StringComparer.Ordinal).ToArray();
    }

    internal string[] ResolveLocalizedTexts(
        IReadOnlyList<LocalizedText> texts,
        CatalogPromptContext promptContext,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        var resolved = new List<string>(texts.Count);
        foreach (var text in texts)
        {
            if (LocalizedTextResolver.TryResolve(
                    text,
                    isoCode,
                    promptContext.DefaultLanguage,
                    promptContext.SupportedLanguages,
                    strictLocalization: false,
                    politenessLevel,
                    out var localized)
                && !string.IsNullOrWhiteSpace(localized))
            {
                resolved.Add(TextNormalizer.NormalizeForPhraseMatching(localized));
            }
        }

        return resolved.Distinct(StringComparer.Ordinal).ToArray();
    }
}
