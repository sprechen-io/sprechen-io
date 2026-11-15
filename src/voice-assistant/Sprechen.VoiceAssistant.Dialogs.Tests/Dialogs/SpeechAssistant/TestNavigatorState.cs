using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

internal sealed class TestNavigatorState : IFormNavigatorState
{
    private readonly ImmutableArray<string> _orderedFieldIds;
    private readonly ImmutableDictionary<string, int> _fieldIndexById;
    private readonly ImmutableDictionary<string, string> _fieldToFieldGroupId;
    private readonly ImmutableDictionary<string, string?> _parentByFieldGroupId;
    private readonly HashSet<string> _availableFields;
    private readonly HashSet<string> _audiblyOnlyFields = new(StringComparer.Ordinal);

    public TestNavigatorState(
        ImmutableArray<string> orderedFieldIds,
        ImmutableDictionary<string, int> fieldIndexById,
        ImmutableDictionary<string, string> fieldToFieldGroupId,
        ImmutableDictionary<string, string?> parentByFieldGroupId)
    {
        _orderedFieldIds = orderedFieldIds;
        _fieldIndexById = fieldIndexById;
        _fieldToFieldGroupId = fieldToFieldGroupId;
        _parentByFieldGroupId = parentByFieldGroupId;
        _availableFields = new HashSet<string>(orderedFieldIds, StringComparer.Ordinal);
    }

    public string? CurrentFocusedFieldId { get; private set; }

    public ImmutableArray<string> OrderedFieldIds => _orderedFieldIds;

    public void SetCurrentFocus(string fieldId) => CurrentFocusedFieldId = fieldId;

    public void SetFieldAvailable(string fieldId, bool available)
    {
        if (available)
        {
            _availableFields.Add(fieldId);
        }
        else
        {
            _availableFields.Remove(fieldId);
        }
    }

    public bool TryGetFieldIndex(string fieldId, out int index) => _fieldIndexById.TryGetValue(fieldId, out index);

    public bool IsFieldInputAvailable(string fieldId) => _availableFields.Contains(fieldId);

    public bool IsFieldAudiblyAvailable(string fieldId)
        => _availableFields.Contains(fieldId) || _audiblyOnlyFields.Contains(fieldId);

    public void SetFieldAudiblyOnly(string fieldId)
    {
        _availableFields.Remove(fieldId);
        _audiblyOnlyFields.Add(fieldId);
    }

    public bool IsKnownField(string fieldId) => _fieldIndexById.ContainsKey(fieldId);

    public bool IsKnownFieldGroup(string fieldGroupId) => _parentByFieldGroupId.ContainsKey(fieldGroupId);

    public string? GetFieldGroupId(string fieldId) =>
        _fieldToFieldGroupId.TryGetValue(fieldId, out var fieldGroupId) ? fieldGroupId : null;

    public bool IsFieldGroupOnPath(string fieldGroupId, string targetFieldGroupId)
    {
        var current = fieldGroupId;
        while (!string.IsNullOrEmpty(current))
        {
            if (string.Equals(current, targetFieldGroupId, StringComparison.Ordinal))
            {
                return true;
            }

            if (!_parentByFieldGroupId.TryGetValue(current, out var parent))
            {
                break;
            }

            current = parent;
        }

        return false;
    }

    FieldId? IFormNavigatorState.CurrentFocusedFieldId
        => CurrentFocusedFieldId is { } focusedFieldId ? FieldId.From(focusedFieldId) : null;

    ImmutableArray<string> IFormNavigatorState.OrderedFieldIds => OrderedFieldIds;

    void IFormNavigatorState.SetCurrentFocus(FieldId fieldId) => SetCurrentFocus(fieldId);

    bool IFormNavigatorState.TryGetFieldIndex(FieldId fieldId, out int index) => TryGetFieldIndex(fieldId, out index);

    bool IFormNavigatorState.IsFieldInputAvailable(FieldId fieldId) => IsFieldInputAvailable(fieldId);

    bool IFormNavigatorState.IsFieldAudiblyAvailable(FieldId fieldId) => IsFieldAudiblyAvailable(fieldId);

    bool IFormNavigatorState.IsKnownField(FieldId fieldId) => IsKnownField(fieldId);

    bool IFormNavigatorState.IsKnownFieldGroup(FieldGroupId fieldGroupId) => IsKnownFieldGroup(fieldGroupId);

    FieldGroupId? IFormNavigatorState.GetFieldGroupId(FieldId fieldId)
        => GetFieldGroupId(fieldId) is { } fieldGroupId ? FieldGroupId.From(fieldGroupId) : null;

    bool IFormNavigatorState.IsFieldGroupOnPath(FieldGroupId fieldGroupId, FieldGroupId targetFieldGroupId)
        => IsFieldGroupOnPath(fieldGroupId, targetFieldGroupId);
}


