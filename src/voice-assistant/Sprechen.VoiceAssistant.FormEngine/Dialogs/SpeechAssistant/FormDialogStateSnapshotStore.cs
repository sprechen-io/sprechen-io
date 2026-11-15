using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Kopiert die veränderlichen Zustandsbereiche samt Fokus in eine Momentaufnahme, damit ein Regelablauf seinen Ausgangsstand wiederherstellen kann.
/// </summary>
internal static class FormDialogStateSnapshotStore
{
    public static FormDialogStateSnapshot Take(
        IReadOnlyDictionary<string, string> valuesByVariableId,
        IReadOnlyDictionary<FieldGroupId, bool> fieldGroupVisibleById,
        IReadOnlyDictionary<FieldId, bool> fieldVisibleById,
        IReadOnlyDictionary<FieldId, bool> fieldEnabledById,
        IReadOnlyDictionary<FieldId, bool> fieldReadOnlyById,
        IReadOnlyDictionary<FieldId, string> fieldInputModeOverrides,
        string? focusedFieldId)
    {
        ArgumentNullException.ThrowIfNull(valuesByVariableId);
        ArgumentNullException.ThrowIfNull(fieldGroupVisibleById);
        ArgumentNullException.ThrowIfNull(fieldVisibleById);
        ArgumentNullException.ThrowIfNull(fieldEnabledById);
        ArgumentNullException.ThrowIfNull(fieldReadOnlyById);
        ArgumentNullException.ThrowIfNull(fieldInputModeOverrides);

        return new FormDialogStateSnapshot(
            ValuesByVariableId: valuesByVariableId.ToDictionary(StringComparer.Ordinal),
            FieldGroupVisibleById: fieldGroupVisibleById.ToDictionary(),
            FieldVisibleById: fieldVisibleById.ToDictionary(),
            FieldEnabledById: fieldEnabledById.ToDictionary(),
            FieldReadOnlyById: fieldReadOnlyById.ToDictionary(),
            FieldInputModeOverrides: fieldInputModeOverrides.ToDictionary(),
            FocusedFieldId: focusedFieldId);
    }
}
