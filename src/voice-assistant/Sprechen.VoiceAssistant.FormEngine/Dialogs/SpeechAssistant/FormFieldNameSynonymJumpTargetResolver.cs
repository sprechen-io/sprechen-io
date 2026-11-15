using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Löst ein normalisiertes Feldsynonym auf das erste passende verfügbare Feld auf, damit alternative Zielnamen dieselbe Navigation erreichen.
/// </summary>
internal sealed class FormFieldNameSynonymJumpTargetResolver
{
    private readonly ImmutableArray<string> orderedFieldIds;
    private readonly IReadOnlyDictionary<string, SynonymSet>? fieldNameSynonyms;
    private readonly FormJumpTargetAvailability availability;
    private readonly FormJumpTargetTextMatcher textMatcher;

    public FormFieldNameSynonymJumpTargetResolver(
        ImmutableArray<string> orderedFieldIds,
        IReadOnlyDictionary<string, SynonymSet>? fieldNameSynonyms,
        FormJumpTargetAvailability availability,
        FormJumpTargetTextMatcher textMatcher)
    {
        this.orderedFieldIds = orderedFieldIds;
        this.fieldNameSynonyms = fieldNameSynonyms;
        this.availability = availability ?? throw new ArgumentNullException(nameof(availability));
        this.textMatcher = textMatcher ?? throw new ArgumentNullException(nameof(textMatcher));
    }

    public bool TryResolve(string normalizedTarget, out FormJumpTargetResolution? resolution)
    {
        if (fieldNameSynonyms is null || fieldNameSynonyms.Count == 0)
        {
            resolution = null;
            return false;
        }

        foreach (var fieldId in orderedFieldIds)
        {
            if (!availability.IsNavigableField(fieldId))
            {
                continue;
            }

            if (!fieldNameSynonyms.TryGetValue(fieldId, out var synonymSet) || synonymSet is null)
            {
                continue;
            }

            if (MatchesSynonymSet(synonymSet, normalizedTarget))
            {
                resolution = new FormJumpTargetResolution(FormJumpTargetKind.Field, fieldId);
                return true;
            }
        }

        resolution = null;
        return false;
    }

    private bool MatchesSynonymSet(SynonymSet synonymSet, string normalizedTarget)
    {
        if (textMatcher.Matches(synonymSet.PrimaryName, normalizedTarget))
        {
            return true;
        }

        if (synonymSet.Synonyms is null)
        {
            return false;
        }

        foreach (var synonym in synonymSet.Synonyms)
        {
            if (textMatcher.Matches(synonym, normalizedTarget))
            {
                return true;
            }
        }

        return false;
    }
}
