using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormEditing;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Cqrs.FormEditing;

internal sealed class TestFormState : IEditableFormState
{
    private readonly HashSet<string> _knownFields;
    private readonly Dictionary<string, string> _fieldToVariable = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);

    public TestFormState(string[] knownFields)
    {
        _knownFields = knownFields.ToHashSet(StringComparer.Ordinal);
    }

    public void SetFieldMapping(string fieldId, string variableId)
    {
        _fieldToVariable[fieldId] = variableId;
    }

    public void SetValueDirect(string variableId, string value)
    {
        _values[variableId] = value;
    }

    public string? GetValue(string variableId)
    {
        return _values.TryGetValue(variableId, out var value) ? value : null;
    }

    public bool TryGetVariableId(FieldId fieldId, out VariableId variableId)
    {
        if (_fieldToVariable.TryGetValue(fieldId, out var raw))
        {
            variableId = VariableId.From(raw);
            return true;
        }

        variableId = default;
        return false;
    }

    public string? GetCurrentValue(VariableId variableId)
    {
        return _values.TryGetValue(variableId, out var value) ? value : null;
    }

    public void SetValue(VariableId variableId, string value)
    {
        _values[variableId] = value;
    }

    public bool IsFieldKnown(FieldId fieldId) => _knownFields.Contains(fieldId);
}


