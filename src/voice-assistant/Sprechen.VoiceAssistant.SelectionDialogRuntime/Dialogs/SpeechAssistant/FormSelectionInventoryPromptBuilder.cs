using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.SelectionDialogRuntime.Dialogs.SpeechAssistant;

/// <summary>
/// Baut Ansagen für die verfügbaren Formulare und für mehrdeutige Auswahlergebnisse.
/// Direkt genannte Kandidaten behalten ihre stabilen Nummern. Bei größeren Beständen
/// wird zunächst eine Eingrenzung oder die vollständige Auflistung angeboten.
/// </summary>
internal sealed class FormSelectionInventoryPromptBuilder
{
    internal SelectionDialogPrompt GetAnnouncingPrompt(
        CatalogPromptContext promptContext,
        IReadOnlyList<SelectionCandidate<FormInfo>> candidates,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(promptContext);
        ArgumentNullException.ThrowIfNull(candidates);

        if (candidates.Count == 0)
        {
            return SelectionDialogPrompt.FromText(string.Empty);
        }

        if (candidates.Count > SelectionCandidateAnnouncementThresholds.MaxDirectlyAnnouncedCandidates)
        {
            return GetAmbiguityPrompt(promptContext, candidates, isoCode, politenessLevel);
        }

        if (candidates.Count > SelectionCandidateAnnouncementThresholds.SingleCandidateCount)
        {
            return BuildFormCandidateInventoryPrompt(
                promptContext,
                candidates,
                CatalogDialogPromptIds.FormAnnouncing,
                isoCode,
                politenessLevel);
        }

        return SelectionDialogPrompt.FromText(
            CatalogPromptComposer.ResolvePromptOrThrow(
                promptContext,
                CatalogDialogPromptIds.FormAnnouncing,
                isoCode,
                politenessLevel,
                CreateCountParameters(candidates.Count)));
    }

    internal SelectionDialogPrompt GetSingleFormPrompt(
        CatalogPromptContext promptContext,
        FormInfo form,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(form);
        return SelectionDialogPrompt.FromText(
            CatalogPromptComposer.ResolvePromptOrThrow(
                promptContext,
                CatalogDialogPromptIds.FormSingle,
                isoCode,
                politenessLevel,
                new Dictionary<string, string?>
                {
                    [CatalogPromptTemplateParameterNames.Name] = form.DisplayName
                }));
    }

    internal SelectionDialogPrompt GetListPrompt(
        CatalogPromptContext promptContext,
        IReadOnlyList<SelectionCandidate<FormInfo>> candidates,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(promptContext);
        ArgumentNullException.ThrowIfNull(candidates);

        return BuildFormCandidateInventoryPrompt(
            promptContext,
            candidates,
            CatalogDialogPromptIds.FormListIntro,
            isoCode,
            politenessLevel);
    }

    internal SelectionDialogPrompt GetAmbiguityPrompt(
        CatalogPromptContext promptContext,
        IReadOnlyList<SelectionCandidate<FormInfo>> candidates,
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
                    CatalogDialogPromptIds.FormAmbiguityRefineOrList,
                    isoCode,
                    politenessLevel,
                    CreateCountParameters(candidates.Count)));
        }

        return BuildFormCandidateInventoryPrompt(
            promptContext,
            candidates,
            CatalogDialogPromptIds.FormAmbiguityShortIntro,
            isoCode,
            politenessLevel);
    }

    internal SelectionDialogPrompt GetPrerequisitesNotMetPrompt(
        CatalogPromptContext promptContext,
        FormInfo form,
        IReadOnlyList<string> missingPrerequisites,
        string isoCode,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(promptContext);
        ArgumentNullException.ThrowIfNull(form);
        ArgumentNullException.ThrowIfNull(missingPrerequisites);

        var missingText = string.Join(", ", missingPrerequisites.Where(value => !string.IsNullOrWhiteSpace(value)));
        return SelectionDialogPrompt.FromText(
            CatalogPromptComposer.ResolvePromptOrThrow(
                promptContext,
                CatalogDialogPromptIds.FormPrerequisitesNotMet,
                isoCode,
                politenessLevel,
                new Dictionary<string, string?>
                {
                    [CatalogPromptTemplateParameterNames.Name] = form.DisplayName,
                    [CatalogPromptTemplateParameterNames.MissingPrerequisites] = missingText
                }));
    }

    private static SelectionDialogPrompt BuildFormCandidateInventoryPrompt(
        CatalogPromptContext promptContext,
        IReadOnlyList<SelectionCandidate<FormInfo>> candidates,
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
            new CatalogNumberedSequencePromptRequest<SelectionCandidate<FormInfo>>(
                promptContext,
                candidates,
                isoCode,
                politenessLevel,
                new CatalogPromptSegment(
                    introPromptId,
                    CreateCountParameters(candidates.Count)),
                new CatalogPromptItemSegment<SelectionCandidate<FormInfo>>(
                    CatalogDialogPromptIds.FormListItem,
                    static (_, candidate) => new Dictionary<string, string?>
                    {
                        [CatalogPromptTemplateParameterNames.Number] = candidate.StableNumber.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        [CatalogPromptTemplateParameterNames.Name] = candidate.DisplayName
                    }),
                new CatalogPromptSegment(
                    CatalogDialogPromptIds.FormListSelectPrompt)));
    }

    private static IReadOnlyDictionary<string, string?> CreateCountParameters(int count)
    {
        return new Dictionary<string, string?>
        {
            [CatalogPromptTemplateParameterNames.Count] = count.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };
    }
}
