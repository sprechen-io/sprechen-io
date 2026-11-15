using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

internal sealed class TestFormState :
    IFormActionVisibilityState,
    IFormActionFieldAccessState,
    IFormActionVariableValueState,
    IFormActionNavigationState
{
    private readonly Dictionary<string, bool> _fieldVisible = new(StringComparer.Ordinal);
    private readonly Dictionary<string, bool> _sectionVisible = new(StringComparer.Ordinal);
    private readonly Dictionary<string, bool> _fieldEnabled = new(StringComparer.Ordinal);
    private readonly Dictionary<string, bool> _fieldReadOnly = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _variableValues = new(StringComparer.Ordinal);

    public string? LastNavigatedFieldGroupId { get; private set; }

    public string? LastFocusedFieldId { get; private set; }

    public bool IsFieldVisible(string fieldId) => _fieldVisible.GetValueOrDefault(fieldId, true);

    public bool SetFieldVisible(string fieldId, bool visible)
    {
        var previous = _fieldVisible.GetValueOrDefault(fieldId, true);
        _fieldVisible[fieldId] = visible;
        return previous != visible;
    }

    public bool IsFieldGroupVisible(string fieldGroupId) => _sectionVisible.GetValueOrDefault(fieldGroupId, true);

    public bool SetFieldGroupVisible(string fieldGroupId, bool visible)
    {
        var previous = _sectionVisible.GetValueOrDefault(fieldGroupId, true);
        _sectionVisible[fieldGroupId] = visible;
        return previous != visible;
    }

    public bool IsFieldEnabled(string fieldId) => _fieldEnabled.GetValueOrDefault(fieldId, true);

    public bool SetFieldEnabled(string fieldId, bool enabled)
    {
        var previous = _fieldEnabled.GetValueOrDefault(fieldId, true);
        _fieldEnabled[fieldId] = enabled;
        return previous != enabled;
    }

    public bool IsFieldReadOnly(string fieldId) => _fieldReadOnly.GetValueOrDefault(fieldId, false);

    public bool SetFieldReadOnly(string fieldId, bool readOnly)
    {
        var previous = _fieldReadOnly.GetValueOrDefault(fieldId, false);
        _fieldReadOnly[fieldId] = readOnly;
        return previous != readOnly;
    }

    public string? GetVariableValue(string variableId) => _variableValues.GetValueOrDefault(variableId);

    public IReadOnlyDictionary<string, string> GetVariableValuesSnapshot()
        => new Dictionary<string, string>(_variableValues, StringComparer.Ordinal);

    public void ReplaceVariableValues(IReadOnlyDictionary<string, string> valuesByVariableId)
    {
        ArgumentNullException.ThrowIfNull(valuesByVariableId);

        _variableValues.Clear();
        foreach (var pair in valuesByVariableId)
        {
            _variableValues[pair.Key] = pair.Value;
        }
    }

    public bool SetVariableValue(string variableId, string value)
    {
        var previousExists = _variableValues.TryGetValue(variableId, out var previous);
        _variableValues[variableId] = value;
        return !previousExists || !string.Equals(previous, value, StringComparison.Ordinal);
    }

    public bool ClearVariableValue(string variableId) => _variableValues.Remove(variableId);

    public bool TryNavigateToFieldGroup(string fieldGroupId)
    {
        LastNavigatedFieldGroupId = fieldGroupId;
        return true;
    }

    public bool TrySetFocus(string fieldId)
    {
        LastFocusedFieldId = fieldId;
        return true;
    }

    bool IFormActionVisibilityState.IsFieldVisible(FieldId fieldId) => IsFieldVisible(fieldId);

    bool IFormActionVisibilityState.SetFieldVisible(FieldId fieldId, bool visible) => SetFieldVisible(fieldId, visible);

    bool IFormActionVisibilityState.IsFieldGroupVisible(FieldGroupId fieldGroupId) => IsFieldGroupVisible(fieldGroupId);

    bool IFormActionVisibilityState.SetFieldGroupVisible(FieldGroupId fieldGroupId, bool visible) => SetFieldGroupVisible(fieldGroupId, visible);

    bool IFormActionFieldAccessState.IsFieldEnabled(FieldId fieldId) => IsFieldEnabled(fieldId);

    bool IFormActionFieldAccessState.SetFieldEnabled(FieldId fieldId, bool enabled) => SetFieldEnabled(fieldId, enabled);

    bool IFormActionFieldAccessState.IsFieldReadOnly(FieldId fieldId) => IsFieldReadOnly(fieldId);

    bool IFormActionFieldAccessState.SetFieldReadOnly(FieldId fieldId, bool readOnly) => SetFieldReadOnly(fieldId, readOnly);

    string? IFormActionVariableValueState.GetVariableValue(VariableId variableId) => GetVariableValue(variableId);

    bool IFormActionVariableValueState.SetVariableValue(VariableId variableId, string value) => SetVariableValue(variableId, value);

    bool IFormActionVariableValueState.ClearVariableValue(VariableId variableId) => ClearVariableValue(variableId);

    bool IFormActionNavigationState.TryNavigateToFieldGroup(FieldGroupId fieldGroupId) => TryNavigateToFieldGroup(fieldGroupId);

    bool IFormActionNavigationState.TrySetFocus(FieldId fieldId) => TrySetFocus(fieldId);
}
