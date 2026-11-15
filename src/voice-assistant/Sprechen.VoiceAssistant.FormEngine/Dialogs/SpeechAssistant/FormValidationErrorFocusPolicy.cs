using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Sucht anhand geordneter Validierungsverstöße ein fokussierbares Ziel mit Umlauf, damit der Fehlernavigationsbefehl den nächsten passenden Verstoß erhält.
/// </summary>
internal sealed class FormValidationErrorFocusPolicy(
    ImmutableArray<string> orderedFieldIds,
    ImmutableDictionary<FieldId, int> fieldIndexById)
{
    private readonly ImmutableArray<string> _orderedFieldIds = orderedFieldIds;
    private readonly ImmutableDictionary<FieldId, int> _fieldIndexById = fieldIndexById ?? throw new ArgumentNullException(nameof(fieldIndexById));

    public FormConstraintViolation? TryFocusNextError(
        string? focusedFieldId,
        ImmutableArray<FormConstraintViolation> violations,
        Func<IEnumerable<FormConstraintViolation>, ImmutableArray<FormConstraintViolation>> orderViolationsForField,
        Func<string, bool> trySetFocus)
    {
        ArgumentNullException.ThrowIfNull(orderViolationsForField);
        ArgumentNullException.ThrowIfNull(trySetFocus);

        if (focusedFieldId is null)
        {
            return null;
        }

        var violationsByField = violations
            .GroupBy(violation => violation.FieldId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => orderViolationsForField(group), StringComparer.Ordinal);

        if (violationsByField.Count == 0)
        {
            return null;
        }

        var startIndex = _fieldIndexById.TryGetValue(FieldId.From(focusedFieldId), out var currentIndex) ? currentIndex : 0;

        var nextViolation = TryFocusFirstViolationInRange(
            startIndex + 1,
            _orderedFieldIds.Length,
            violationsByField,
            trySetFocus);

        return nextViolation ?? TryFocusFirstViolationInRange(
            startIndex: 0,
            endIndexExclusive: _orderedFieldIds.Length,
            violationsByField,
            trySetFocus);
    }

    private FormConstraintViolation? TryFocusFirstViolationInRange(
        int startIndex,
        int endIndexExclusive,
        IReadOnlyDictionary<string, ImmutableArray<FormConstraintViolation>> violationsByField,
        Func<string, bool> trySetFocus)
    {
        for (var index = startIndex; index < endIndexExclusive; index++)
        {
            var fieldId = _orderedFieldIds[index];
            if (!violationsByField.TryGetValue(fieldId, out var fieldViolations))
            {
                continue;
            }

            if (!trySetFocus(fieldId))
            {
                continue;
            }

            return fieldViolations[0];
        }

        return null;
    }
}
