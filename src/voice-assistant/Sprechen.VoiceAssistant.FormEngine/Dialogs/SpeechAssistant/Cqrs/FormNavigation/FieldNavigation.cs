using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormNavigation;

/// <summary>
/// Sucht das nächste verfügbare Nachbarfeld in der geordneten Feldfolge, ohne selbst den
/// Fokus zu verändern.
/// </summary>
internal static class FieldNavigation
{
    public delegate bool TryGetFieldIndex(FieldId fieldId, out int index);

    public static bool TryFindNextAvailableField(
        FieldId? currentFieldId,
        IReadOnlyList<string> orderedFieldIds,
        Func<FieldId, bool> isFieldInputAvailable,
        TryGetFieldIndex tryGetFieldIndex,
        out FieldId? nextFieldId)
    {
        return TryFindAdjacentAvailableField(
            currentFieldId,
            orderedFieldIds,
            isFieldInputAvailable,
            tryGetFieldIndex,
            step: 1,
            out nextFieldId);
    }

    public static bool TryFindPreviousAvailableField(
        FieldId? currentFieldId,
        IReadOnlyList<string> orderedFieldIds,
        Func<FieldId, bool> isFieldInputAvailable,
        TryGetFieldIndex tryGetFieldIndex,
        out FieldId? previousFieldId)
    {
        return TryFindAdjacentAvailableField(
            currentFieldId,
            orderedFieldIds,
            isFieldInputAvailable,
            tryGetFieldIndex,
            step: -1,
            out previousFieldId);
    }

    private static bool TryFindAdjacentAvailableField(
        FieldId? currentFieldId,
        IReadOnlyList<string> orderedFieldIds,
        Func<FieldId, bool> isFieldInputAvailable,
        TryGetFieldIndex tryGetFieldIndex,
        int step,
        out FieldId? targetFieldId)
    {
        ArgumentNullException.ThrowIfNull(orderedFieldIds);
        ArgumentNullException.ThrowIfNull(isFieldInputAvailable);
        ArgumentNullException.ThrowIfNull(tryGetFieldIndex);

        targetFieldId = null;

        if (currentFieldId is not { } current)
        {
            return false;
        }

        if (!tryGetFieldIndex(current, out var currentIndex))
        {
            return false;
        }

        for (var index = currentIndex + step; index >= 0 && index < orderedFieldIds.Count; index += step)
        {
            var candidate = FieldId.From(orderedFieldIds[index]);
            if (!isFieldInputAvailable(candidate))
            {
                continue;
            }

            targetFieldId = candidate;
            return true;
        }

        return false;
    }
}
