using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormNavigation;

/// <summary>
/// Verbindet die Suche nach dem nächsten oder vorherigen verfügbaren Feld mit dem
/// anschließenden Fokuswechsel für die Formularnavigation.
/// </summary>
internal static class FocusNavigationTransitions
{
    public static bool TryMoveNext(
        FieldId? currentFieldId,
        IReadOnlyList<string> orderedFieldIds,
        Func<FieldId, bool> isFieldInputAvailable,
        FieldNavigation.TryGetFieldIndex tryGetFieldIndex,
        Action<FieldId> setCurrentFocus,
        out FieldId? previousFieldId,
        out FieldId? targetFieldId)
    {
        return TryMove(
            currentFieldId,
            orderedFieldIds,
            isFieldInputAvailable,
            tryGetFieldIndex,
            setCurrentFocus,
            FieldNavigation.TryFindNextAvailableField,
            out previousFieldId,
            out targetFieldId);
    }

    public static bool TryMoveBack(
        FieldId? currentFieldId,
        IReadOnlyList<string> orderedFieldIds,
        Func<FieldId, bool> isFieldInputAvailable,
        FieldNavigation.TryGetFieldIndex tryGetFieldIndex,
        Action<FieldId> setCurrentFocus,
        out FieldId? previousFieldId,
        out FieldId? targetFieldId)
    {
        return TryMove(
            currentFieldId,
            orderedFieldIds,
            isFieldInputAvailable,
            tryGetFieldIndex,
            setCurrentFocus,
            FieldNavigation.TryFindPreviousAvailableField,
            out previousFieldId,
            out targetFieldId);
    }

    private static bool TryMove(
        FieldId? currentFieldId,
        IReadOnlyList<string> orderedFieldIds,
        Func<FieldId, bool> isFieldInputAvailable,
        FieldNavigation.TryGetFieldIndex tryGetFieldIndex,
        Action<FieldId> setCurrentFocus,
        TryFindAdjacentField tryFindAdjacentField,
        out FieldId? previousFieldId,
        out FieldId? targetFieldId)
    {
        ArgumentNullException.ThrowIfNull(orderedFieldIds);
        ArgumentNullException.ThrowIfNull(isFieldInputAvailable);
        ArgumentNullException.ThrowIfNull(tryGetFieldIndex);
        ArgumentNullException.ThrowIfNull(setCurrentFocus);
        ArgumentNullException.ThrowIfNull(tryFindAdjacentField);

        previousFieldId = currentFieldId;
        targetFieldId = null;

        if (!tryFindAdjacentField(
                currentFieldId,
                orderedFieldIds,
                isFieldInputAvailable,
                tryGetFieldIndex,
                out var adjacentFieldId)
            || previousFieldId is null
            || adjacentFieldId is not { } adjacent)
        {
            return false;
        }

        targetFieldId = adjacent;
        setCurrentFocus(adjacent);
        return true;
    }

    private delegate bool TryFindAdjacentField(
        FieldId? currentFieldId,
        IReadOnlyList<string> orderedFieldIds,
        Func<FieldId, bool> isFieldInputAvailable,
        FieldNavigation.TryGetFieldIndex tryGetFieldIndex,
        out FieldId? targetFieldId);
}
