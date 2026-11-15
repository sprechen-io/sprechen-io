using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Sucht aus den übergebenen offenen Pflichtfeldern ein anderes Fokusziel in
/// Dokumentreihenfolge und beginnt nach dem Ende erneut vorn. Das Ergebnis liefert
/// Ziel und Anzahl für die anschließende Rückmeldung. Den Fokus setzt der übergebene Aufruf.
/// </summary>
internal sealed class FormOpenRequiredFieldFocusPolicy(
    ImmutableArray<string> orderedFieldIds,
    ImmutableDictionary<FieldId, int> fieldIndexById)
{
    private readonly ImmutableArray<string> _orderedFieldIds = orderedFieldIds;
    private readonly ImmutableDictionary<FieldId, int> _fieldIndexById = fieldIndexById ?? throw new ArgumentNullException(nameof(fieldIndexById));

    public FocusNextOpenRequiredFieldResult TryFocusNextOpenRequired(
        string? focusedFieldId,
        ImmutableArray<string> openRequiredFieldIds,
        Func<string, bool> trySetFocus)
    {
        ArgumentNullException.ThrowIfNull(trySetFocus);

        var remainingCount = openRequiredFieldIds.Length;
        if (remainingCount == 0)
        {
            return new FocusNextOpenRequiredFieldResult(FocusNextOpenRequiredFieldResultKind.NoOpenRequiredField, TargetFieldId: null, RemainingOpenRequiredCount: 0);
        }

        var openRequiredSet = openRequiredFieldIds.ToImmutableHashSet(StringComparer.Ordinal);

        if (remainingCount == 1 && focusedFieldId is not null && openRequiredSet.Contains(focusedFieldId))
        {
            return new FocusNextOpenRequiredFieldResult(FocusNextOpenRequiredFieldResultKind.AlreadyOnSoleTarget, focusedFieldId, remainingCount);
        }

        var startIndex = focusedFieldId is not null && _fieldIndexById.TryGetValue(FieldId.From(focusedFieldId), out var currentIndex)
            ? currentIndex
            : -1;

        var forwardTarget = TryFocusFirstOpenRequiredInRange(startIndex + 1, _orderedFieldIds.Length, openRequiredSet, focusedFieldId, trySetFocus);
        if (forwardTarget is not null)
        {
            return new FocusNextOpenRequiredFieldResult(FocusNextOpenRequiredFieldResultKind.Moved, forwardTarget, remainingCount);
        }

        var wrappedTarget = TryFocusFirstOpenRequiredInRange(startIndex: 0, _orderedFieldIds.Length, openRequiredSet, focusedFieldId, trySetFocus);
        if (wrappedTarget is not null)
        {
            return new FocusNextOpenRequiredFieldResult(FocusNextOpenRequiredFieldResultKind.WrappedToStart, wrappedTarget, remainingCount);
        }

        // Kein anderes Ziel konnte übernommen werden. Das Ergebnis kennzeichnet diesen
        // Fall auch dann, wenn sämtliche angebotenen Ziele nicht fokussierbar waren.
        return new FocusNextOpenRequiredFieldResult(FocusNextOpenRequiredFieldResultKind.AlreadyOnSoleTarget, focusedFieldId, remainingCount);
    }

    private string? TryFocusFirstOpenRequiredInRange(
        int startIndex,
        int endIndexExclusive,
        ImmutableHashSet<string> openRequiredSet,
        string? skipFieldId,
        Func<string, bool> trySetFocus)
    {
        for (var index = Math.Max(startIndex, 0); index < endIndexExclusive; index++)
        {
            var fieldId = _orderedFieldIds[index];
            if (string.Equals(fieldId, skipFieldId, StringComparison.Ordinal))
            {
                continue;
            }

            if (!openRequiredSet.Contains(fieldId))
            {
                continue;
            }

            if (!trySetFocus(fieldId))
            {
                continue;
            }

            return fieldId;
        }

        return null;
    }
}
