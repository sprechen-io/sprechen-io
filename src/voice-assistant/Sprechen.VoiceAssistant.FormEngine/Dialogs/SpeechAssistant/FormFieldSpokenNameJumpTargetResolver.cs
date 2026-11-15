using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Löst einen normalisierten gesprochenen Feldnamen auf das erste passende verfügbare Feld auf, damit modellierte Sprachnamen als Sprungziel dienen.
/// </summary>
internal sealed class FormFieldSpokenNameJumpTargetResolver
{
    private readonly ImmutableArray<string> orderedFieldIds;
    private readonly ImmutableDictionary<FieldId, FieldDefinition> fieldsById;
    private readonly FormJumpTargetAvailability availability;
    private readonly FormJumpTargetTextMatcher textMatcher;

    public FormFieldSpokenNameJumpTargetResolver(
        ImmutableArray<string> orderedFieldIds,
        ImmutableDictionary<FieldId, FieldDefinition> fieldsById,
        FormJumpTargetAvailability availability,
        FormJumpTargetTextMatcher textMatcher)
    {
        this.orderedFieldIds = orderedFieldIds;
        this.fieldsById = fieldsById ?? throw new ArgumentNullException(nameof(fieldsById));
        this.availability = availability ?? throw new ArgumentNullException(nameof(availability));
        this.textMatcher = textMatcher ?? throw new ArgumentNullException(nameof(textMatcher));
    }

    public bool TryResolve(string normalizedTarget, out FormJumpTargetResolution? resolution)
    {
        foreach (var fieldId in orderedFieldIds)
        {
            if (!fieldsById.TryGetValue(FieldId.From(fieldId), out var fieldDefinition))
            {
                continue;
            }

            if (!availability.IsNavigableField(fieldId))
            {
                continue;
            }

            if (textMatcher.Matches(fieldDefinition.Projections?.Voice?.Names, normalizedTarget))
            {
                resolution = new FormJumpTargetResolution(FormJumpTargetKind.Field, fieldId);
                return true;
            }
        }

        resolution = null;
        return false;
    }
}
