using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet eine normalisierte Feldbezeichnung dem ersten passenden verfügbaren Feld zu.
/// </summary>
internal sealed class FormFieldLabelJumpTargetResolver
{
    private readonly ImmutableArray<string> orderedFieldIds;
    private readonly ImmutableDictionary<FieldId, FieldDefinition> fieldsById;
    private readonly FormJumpTargetAvailability availability;
    private readonly FormJumpTargetTextMatcher textMatcher;

    public FormFieldLabelJumpTargetResolver(
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

            if (textMatcher.Matches(fieldDefinition.Label, normalizedTarget))
            {
                resolution = new FormJumpTargetResolution(FormJumpTargetKind.Field, fieldId);
                return true;
            }
        }

        resolution = null;
        return false;
    }
}
