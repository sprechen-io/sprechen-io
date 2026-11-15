using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormNavigation;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Cqrs.FormNavigation.NavigateNextCommandHandlerTestsSupport;

internal sealed class TestFormState : INavigableFormState
{
    private readonly string[] _orderedFieldIds;
    private readonly HashSet<string> _invisibleFields;

    public TestFormState(string[] orderedFieldIds, string? currentFocus, string[]? invisibleFields = null)
    {
        _orderedFieldIds = orderedFieldIds;
        CurrentFocusedFieldId = currentFocus;
        _invisibleFields = invisibleFields?.ToHashSet(StringComparer.Ordinal) ?? [];
    }

    public string? CurrentFocusedFieldId { get; private set; }

    public string[] OrderedFieldIds => _orderedFieldIds;

    public bool IsFieldInputAvailable(string fieldId) => !_invisibleFields.Contains(fieldId);

    public void SetCurrentFocus(string fieldId) => CurrentFocusedFieldId = fieldId;

    public bool TryGetFieldIndex(string fieldId, out int index)
    {
        index = Array.IndexOf(_orderedFieldIds, fieldId);
        return index >= 0;
    }

    FieldId? INavigableFormState.CurrentFocusedFieldId
        => CurrentFocusedFieldId is { } focusedFieldId ? FieldId.From(focusedFieldId) : null;

    bool INavigableFormState.IsFieldInputAvailable(FieldId fieldId) => IsFieldInputAvailable(fieldId);

    void INavigableFormState.SetCurrentFocus(FieldId fieldId) => SetCurrentFocus(fieldId);

    bool INavigableFormState.TryGetFieldIndex(FieldId fieldId, out int index) => TryGetFieldIndex(fieldId, out index);
}


