using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Baut Ansagen für verfügbare Anwendungen und mehrdeutige Auswahlergebnisse mit stabilen
/// Kandidatennummern. Größere Auswahlen fordern zunächst zur Eingrenzung oder Auflistung auf.
/// </summary>
internal sealed class FormsCatalogInventoryPromptBuilder
{
    internal SelectionDialogPrompt GetAnnouncingPrompt(
        CatalogPromptContext promptContext,
        IReadOnlyList<SelectionCandidate<ApplicationInfo>> candidates,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(promptContext);
        if (candidates is null || candidates.Count == 0)
        {
            return SelectionDialogPrompt.FromText(string.Empty);
        }

        if (candidates.Count == SelectionCandidateAnnouncementThresholds.SingleCandidateCount)
        {
            return SelectionDialogPrompt.FromText(
                CatalogPromptComposer.ResolvePromptOrThrow(
                    promptContext,
                    CatalogDialogPromptIds.AppAnnouncingSingle,
                    isoCode,
                    politenessLevel,
                    new Dictionary<string, string?>
                    {
                        [CatalogPromptTemplateParameterNames.Name] = candidates[0].DisplayName
                    }));
        }

        return candidates.Count > SelectionCandidateAnnouncementThresholds.MaxDirectlyAnnouncedCandidates
            ? GetAmbiguityPrompt(promptContext, candidates, isoCode, politenessLevel)
            : BuildApplicationCandidateInventoryPrompt(
                promptContext,
                candidates,
                CatalogDialogPromptIds.AppAnnouncingMultiple,
                isoCode,
                politenessLevel);
    }

    internal SelectionDialogPrompt GetListPrompt(
        CatalogPromptContext promptContext,
        IReadOnlyList<SelectionCandidate<ApplicationInfo>> candidates,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(promptContext);
        if (candidates is null || candidates.Count == 0)
        {
            return SelectionDialogPrompt.FromText(string.Empty);
        }

        return BuildApplicationCandidateInventoryPrompt(
            promptContext,
            candidates,
            CatalogDialogPromptIds.AppListIntro,
            isoCode,
            politenessLevel);
    }

    internal SelectionDialogPrompt GetAmbiguityPrompt(
        CatalogPromptContext promptContext,
        IReadOnlyList<SelectionCandidate<ApplicationInfo>> candidates,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(promptContext);
        ArgumentNullException.ThrowIfNull(candidates);
        if (candidates.Count < SelectionCandidateAnnouncementThresholds.MinAmbiguityCandidateCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(candidates),
                candidates.Count,
                "An ambiguity prompt requires at least two candidates.");
        }

        if (candidates.Count > SelectionCandidateAnnouncementThresholds.MaxDirectlyAnnouncedCandidates)
        {
            return SelectionDialogPrompt.FromText(
                CatalogPromptComposer.ResolvePromptOrThrow(
                    promptContext,
                    CatalogDialogPromptIds.AppAmbiguityRefineOrList,
                    isoCode,
                    politenessLevel,
                    CreateCountParameters(candidates.Count)));
        }

        return BuildApplicationCandidateInventoryPrompt(
            promptContext,
            candidates,
            CatalogDialogPromptIds.AppAmbiguityShortIntro,
            isoCode,
            politenessLevel);
    }

    private static SelectionDialogPrompt BuildApplicationCandidateInventoryPrompt(
        CatalogPromptContext promptContext,
        IReadOnlyList<SelectionCandidate<ApplicationInfo>> candidates,
        string introPromptId,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(promptContext);
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentException.ThrowIfNullOrWhiteSpace(introPromptId);
        ArgumentException.ThrowIfNullOrWhiteSpace(isoCode);

        if (candidates.Count == 0)
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
                    introPromptId,
                    CreateCountParameters(candidates.Count)),
                new CatalogPromptItemSegment<SelectionCandidate<ApplicationInfo>>(
                    CatalogDialogPromptIds.AppListItem,
                    static (_, candidate) => new Dictionary<string, string?>
                    {
                        [CatalogPromptTemplateParameterNames.Number] = candidate.StableNumber.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        [CatalogPromptTemplateParameterNames.Name] = candidate.DisplayName
                    }),
                new CatalogPromptSegment(
                    CatalogDialogPromptIds.AppListSelectPrompt)));
    }

    private static IReadOnlyDictionary<string, string?> CreateCountParameters(int count)
    {
        return new Dictionary<string, string?>
        {
            [CatalogPromptTemplateParameterNames.Count] = count.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };
    }
}
