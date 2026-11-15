using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Behavior.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Hält veränderliche Formulardaten für Regel- und Projektionsprüfungen im Speicher.
/// Regelausführung, Snapshot und Sichtbarkeitsabfragen greifen auf dieselben Dictionaries zu.
/// Änderungsmethoden melden, ob sie den gespeicherten Wert geändert haben, damit die Regelschleife Konvergenz erkennen kann.
/// </summary>
internal sealed class RecordingFormDialogState :
    IFormActionVisibilityState,
    IFormActionFieldAccessState,
    IFormActionVariableValueState,
    IFormActionNavigationState
{
    public Dictionary<string, string> ValuesByVariableId { get; } = new(StringComparer.Ordinal);

    public Dictionary<FieldGroupId, bool> FieldGroupVisibleById { get; } = [];

    public Dictionary<FieldId, bool> FieldVisibleById { get; } = [];

    public Dictionary<FieldId, bool> FieldEnabledById { get; } = [];

    public Dictionary<FieldId, bool> FieldReadOnlyById { get; } = [];

    public Dictionary<FieldId, string> FieldInputModeOverrides { get; } = [];

    public string? FocusedFieldId { get; set; }

    /// <summary>
    /// Ersetzt die hier gespeicherten Formulardaten vollständig durch den Snapshot für die Wiederherstellung nach Regelabbrüchen.
    /// </summary>
    public void RestoreSnapshot(FormDialogStateSnapshot snapshot)
    {
        ReplaceContents(ValuesByVariableId, snapshot.ValuesByVariableId);
        ReplaceContents(FieldGroupVisibleById, snapshot.FieldGroupVisibleById);
        ReplaceContents(FieldVisibleById, snapshot.FieldVisibleById);
        ReplaceContents(FieldEnabledById, snapshot.FieldEnabledById);
        ReplaceContents(FieldReadOnlyById, snapshot.FieldReadOnlyById);
        ReplaceContents(FieldInputModeOverrides, snapshot.FieldInputModeOverrides);
        FocusedFieldId = snapshot.FocusedFieldId;
    }

    private static void ReplaceContents<TKey, TValue>(
        Dictionary<TKey, TValue> target,
        IReadOnlyDictionary<TKey, TValue> source)
        where TKey : notnull
    {
        target.Clear();
        foreach (var (key, value) in source)
        {
            target[key] = value;
        }
    }

    public bool IsFieldVisible(FieldId fieldId)
        => !FieldVisibleById.TryGetValue(fieldId, out var visible) || visible;

    public bool SetFieldVisible(FieldId fieldId, bool visible)
    {
        if (FieldVisibleById.TryGetValue(fieldId, out var current) && current == visible)
        {
            return false;
        }

        FieldVisibleById[fieldId] = visible;
        return true;
    }

    public bool IsFieldGroupVisible(FieldGroupId fieldGroupId)
        => !FieldGroupVisibleById.TryGetValue(fieldGroupId, out var visible) || visible;

    public bool SetFieldGroupVisible(FieldGroupId fieldGroupId, bool visible)
    {
        if (FieldGroupVisibleById.TryGetValue(fieldGroupId, out var current) && current == visible)
        {
            return false;
        }

        FieldGroupVisibleById[fieldGroupId] = visible;
        return true;
    }

    public bool IsFieldEnabled(FieldId fieldId)
        => !FieldEnabledById.TryGetValue(fieldId, out var enabled) || enabled;

    public bool SetFieldEnabled(FieldId fieldId, bool enabled)
    {
        if (FieldEnabledById.TryGetValue(fieldId, out var current) && current == enabled)
        {
            return false;
        }

        FieldEnabledById[fieldId] = enabled;
        return true;
    }

    public bool IsFieldReadOnly(FieldId fieldId)
        => FieldReadOnlyById.TryGetValue(fieldId, out var readOnly) && readOnly;

    public bool SetFieldReadOnly(FieldId fieldId, bool readOnly)
    {
        if (FieldReadOnlyById.TryGetValue(fieldId, out var current) && current == readOnly)
        {
            return false;
        }

        FieldReadOnlyById[fieldId] = readOnly;
        return true;
    }

    public string? GetVariableValue(VariableId variableId)
        => ValuesByVariableId.TryGetValue(variableId.Value, out var value) ? value : null;

    public bool SetVariableValue(VariableId variableId, string value)
    {
        if (ValuesByVariableId.TryGetValue(variableId.Value, out var current)
            && string.Equals(current, value, StringComparison.Ordinal))
        {
            return false;
        }

        ValuesByVariableId[variableId.Value] = value;
        return true;
    }

    public bool ClearVariableValue(VariableId variableId)
        => ValuesByVariableId.Remove(variableId.Value);

    public bool TryNavigateToFieldGroup(FieldGroupId fieldGroupId) => false;

    public bool TrySetFocus(FieldId fieldId) => false;
}
