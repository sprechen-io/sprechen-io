using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Erstellt Ansagen für Suchtreffer, erfolglose Suchen und fehlende Beschreibungen aus dem
/// Katalog. Trefferlisten verwenden die stabilen Kandidatennummern der Auswahl statt einer neuen
/// Suchnummerierung.
/// </summary>
internal sealed class FormsCatalogSearchPromptBuilder
{
    internal SelectionDialogPrompt GetSearchResultsPrompt(
        CatalogPromptContext promptContext,
        IReadOnlyList<SelectionCandidate<ApplicationInfo>> candidates,
        string? query,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(promptContext);
        if (candidates is null || candidates.Count == 0)
        {
            return SelectionDialogPrompt.FromText(string.Empty);
        }

        return CatalogPromptComposer.BuildNumberedSequenceSelectionPrompt(
            new CatalogNumberedSequencePromptRequest<SelectionCandidate<ApplicationInfo>>(
                promptContext,
                candidates,
                isoCode,
                politenessLevel,
                new CatalogPromptSegment(
                    CatalogDialogPromptIds.AppSearchResultsIntro,
                    new Dictionary<string, string?>
                    {
                        [CatalogPromptTemplateParameterNames.Count] = candidates.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        [CatalogPromptTemplateParameterNames.Query] = query
                    }),
                new CatalogPromptItemSegment<SelectionCandidate<ApplicationInfo>>(
                    CatalogDialogPromptIds.AppSearchResultsItem,
                    static (_, candidate) => new Dictionary<string, string?>
                    {
                        [CatalogPromptTemplateParameterNames.Number] = candidate.StableNumber.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        [CatalogPromptTemplateParameterNames.Name] = candidate.DisplayName
                    }),
                new CatalogPromptSegment(
                    CatalogDialogPromptIds.AppListSelectPrompt)));
    }

    internal SelectionDialogPrompt GetNoSearchResultsPrompt(
        CatalogPromptContext promptContext,
        string? query,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(promptContext);
        var normalizedQuery = string.IsNullOrWhiteSpace(query) ? string.Empty : query.Trim();
        return SelectionDialogPrompt.FromText(
            CatalogPromptComposer.ResolvePromptOrThrow(
                promptContext,
                CatalogDialogPromptIds.AppNoSearchResults,
                isoCode,
                politenessLevel,
                new Dictionary<string, string?>
                {
                    [CatalogPromptTemplateParameterNames.Query] = normalizedQuery
                }));
    }

    internal SelectionDialogPrompt GetNoDescriptionPrompt(
        CatalogPromptContext promptContext,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(promptContext);
        return SelectionDialogPrompt.FromText(
            CatalogPromptComposer.ResolvePromptOrThrow(
                promptContext,
                CatalogDialogPromptIds.AppNoDescription,
                isoCode,
                politenessLevel));
    }
}
