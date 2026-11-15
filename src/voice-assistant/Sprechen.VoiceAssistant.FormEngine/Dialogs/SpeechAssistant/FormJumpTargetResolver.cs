using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Speech.App.Contracts.Services;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft gesprochene Feldnamen, Bezeichnungen, Gruppentitel und Synonyme in fester Folge, damit mehrere Auflösungswege eine gemeinsame Rangfolge haben.
/// </summary>
internal sealed class FormJumpTargetResolver
{
    private readonly FormFieldSpokenNameJumpTargetResolver fieldSpokenNameResolver;
    private readonly FormFieldLabelJumpTargetResolver fieldLabelResolver;
    private readonly FormFieldGroupTitleJumpTargetResolver fieldGroupTitleResolver;
    private readonly FormFieldNameSynonymJumpTargetResolver fieldNameSynonymResolver;

    public FormJumpTargetResolver(
        ImmutableArray<string> orderedFieldIds,
        ImmutableDictionary<FieldId, FieldDefinition> fieldsById,
        ImmutableArray<string> orderedFieldGroupIds,
        IReadOnlyDictionary<string, SynonymSet>? fieldNameSynonyms,
        FormLocalizationResolver localizationResolver,
        IFormNavigatorState navigatorState)
    {
        ArgumentNullException.ThrowIfNull(fieldsById);
        ArgumentNullException.ThrowIfNull(localizationResolver);
        ArgumentNullException.ThrowIfNull(navigatorState);

        var availability = new FormJumpTargetAvailability(navigatorState);
        var textMatcher = new FormJumpTargetTextMatcher(localizationResolver);
        fieldSpokenNameResolver = new FormFieldSpokenNameJumpTargetResolver(orderedFieldIds, fieldsById, availability, textMatcher);
        fieldLabelResolver = new FormFieldLabelJumpTargetResolver(orderedFieldIds, fieldsById, availability, textMatcher);
        fieldGroupTitleResolver = new FormFieldGroupTitleJumpTargetResolver(orderedFieldGroupIds, localizationResolver, availability, textMatcher);
        fieldNameSynonymResolver = new FormFieldNameSynonymJumpTargetResolver(orderedFieldIds, fieldNameSynonyms, availability, textMatcher);
    }

    public bool TryResolve(FormJumpTarget target, out FormJumpTargetResolution? resolution)
    {
        resolution = null;

        if (string.IsNullOrWhiteSpace(target))
        {
            return false;
        }

        var normalizedTarget = TextNormalizer.NormalizeForPhraseMatching(target);
        if (normalizedTarget.Length == 0)
        {
            return false;
        }

        if (fieldSpokenNameResolver.TryResolve(normalizedTarget, out resolution))
        {
            return true;
        }

        if (fieldLabelResolver.TryResolve(normalizedTarget, out resolution))
        {
            return true;
        }

        if (fieldGroupTitleResolver.TryResolve(normalizedTarget, out resolution))
        {
            return true;
        }

        return fieldNameSynonymResolver.TryResolve(normalizedTarget, out resolution);
    }
}
