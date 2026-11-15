using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormNavigation;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Cqrs.FormNavigation.NavigateToFieldGroupCommandHandlerTestsSupport;

internal sealed class TestFormState : IFieldGroupNavigableFormState
{
    private readonly Dictionary<string, string[]> _fieldGroups;
    private readonly HashSet<string> _invisibleFields;
    private readonly HashSet<string> _invisibleFieldGroups;

    public TestFormState(
        Dictionary<string, string[]> fieldGroups,
        string? currentFocus,
        string[]? invisibleFields = null,
        string[]? invisibleFieldGroups = null)
    {
        _fieldGroups = fieldGroups;
        CurrentFocusedFieldId = currentFocus;
        _invisibleFields = invisibleFields?.ToHashSet(StringComparer.Ordinal) ?? [];
        _invisibleFieldGroups = invisibleFieldGroups?.ToHashSet(StringComparer.Ordinal) ?? [];
    }

    public string? CurrentFocusedFieldId { get; private set; }

    public string? GetCurrentFieldGroupId()
    {
        if (CurrentFocusedFieldId is null)
        {
            return null;
        }

        foreach (var (fieldGroupId, fields) in _fieldGroups)
        {
            if (fields.Contains(CurrentFocusedFieldId, StringComparer.Ordinal))
            {
                return fieldGroupId;
            }
        }

        return null;
    }

    public bool IsFieldGroupKnown(string fieldGroupId) => _fieldGroups.ContainsKey(fieldGroupId);

    public bool IsFieldGroupVisible(string fieldGroupId) => !_invisibleFieldGroups.Contains(fieldGroupId);

    public IEnumerable<string> GetFieldsInFieldGroup(string fieldGroupId)
    {
        return _fieldGroups.TryGetValue(fieldGroupId, out var fields) ? fields : [];
    }

    public bool IsFieldInputAvailable(string fieldId) => !_invisibleFields.Contains(fieldId);

    public void SetCurrentFocus(string fieldId) => CurrentFocusedFieldId = fieldId;

    FieldId? IFieldGroupNavigableFormState.CurrentFocusedFieldId
        => CurrentFocusedFieldId is { } focusedFieldId ? FieldId.From(focusedFieldId) : null;

    FieldGroupId? IFieldGroupNavigableFormState.GetCurrentFieldGroupId()
        => GetCurrentFieldGroupId() is { } fieldGroupId ? FieldGroupId.From(fieldGroupId) : null;

    bool IFieldGroupNavigableFormState.IsFieldGroupKnown(FieldGroupId fieldGroupId) => IsFieldGroupKnown(fieldGroupId);

    bool IFieldGroupNavigableFormState.IsFieldGroupVisible(FieldGroupId fieldGroupId) => IsFieldGroupVisible(fieldGroupId);

    IEnumerable<string> IFieldGroupNavigableFormState.GetFieldsInFieldGroup(FieldGroupId fieldGroupId) => GetFieldsInFieldGroup(fieldGroupId);

    bool IFieldGroupNavigableFormState.IsFieldInputAvailable(FieldId fieldId) => IsFieldInputAvailable(fieldId);

    void IFieldGroupNavigableFormState.SetCurrentFocus(FieldId fieldId) => SetCurrentFocus(fieldId);
}




