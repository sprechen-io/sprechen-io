using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormEditing;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormNavigation;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant.Cqrs.FormStatus;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Verbindet den Zustand mit fünf getrennten Befehls- und Abfragezugängen, damit Aufrufer die jeweils vorgesehene Sicht erhalten.
/// </summary>
internal sealed class FormDialogCqrsStateAdapter :
    INavigableFormState,
    IFieldGroupNavigableFormState,
    IEditableFormState,
    IReadableFormStatus,
    IReadableFormSummary
{
    private readonly FormCqrsProjectionAdapter _projection;

    public FormDialogCqrsStateAdapter(FormCqrsProjectionAdapter projection)
    {
        ArgumentNullException.ThrowIfNull(projection);
        _projection = projection;
    }

    FieldId? INavigableFormState.CurrentFocusedFieldId
        => _projection.Navigation.CurrentFocusedFieldId;

    string[] INavigableFormState.OrderedFieldIds => _projection.Navigation.OrderedFieldIds;

    bool INavigableFormState.IsFieldInputAvailable(FieldId fieldId)
        => _projection.Navigation.IsFieldInputAvailable(fieldId);

    void INavigableFormState.SetCurrentFocus(FieldId fieldId)
        => _projection.Navigation.SetCurrentFocus(fieldId);

    bool INavigableFormState.TryGetFieldIndex(FieldId fieldId, out int index)
        => _projection.Navigation.TryGetFieldIndex(fieldId, out index);

    FieldId? IFieldGroupNavigableFormState.CurrentFocusedFieldId
        => _projection.Navigation.CurrentFocusedFieldId;

    FieldGroupId? IFieldGroupNavigableFormState.GetCurrentFieldGroupId()
        => _projection.Status.CurrentFieldGroupId is { } fieldGroupId ? FieldGroupId.From(fieldGroupId) : null;

    bool IFieldGroupNavigableFormState.IsFieldGroupKnown(FieldGroupId fieldGroupId)
        => _projection.FieldGroupNavigation.IsFieldGroupKnown(fieldGroupId);

    bool IFieldGroupNavigableFormState.IsFieldGroupVisible(FieldGroupId fieldGroupId)
        => _projection.FieldGroupNavigation.IsFieldGroupVisible(fieldGroupId);

    IEnumerable<string> IFieldGroupNavigableFormState.GetFieldsInFieldGroup(FieldGroupId fieldGroupId)
        => _projection.FieldGroupNavigation.GetFieldsInFieldGroup(fieldGroupId);

    bool IFieldGroupNavigableFormState.IsFieldInputAvailable(FieldId fieldId)
        => _projection.Navigation.IsFieldInputAvailable(fieldId);

    void IFieldGroupNavigableFormState.SetCurrentFocus(FieldId fieldId)
        => _projection.Navigation.SetCurrentFocus(fieldId);

    bool IEditableFormState.IsFieldKnown(FieldId fieldId)
        => _projection.Editing.IsFieldKnown(fieldId);

    bool IEditableFormState.TryGetVariableId(FieldId fieldId, out VariableId variableId)
    {
        if (_projection.Editing.TryGetVariableId(fieldId, out var raw))
        {
            variableId = VariableId.From(raw);
            return true;
        }

        variableId = default;
        return false;
    }

    string? IEditableFormState.GetCurrentValue(VariableId variableId)
        => _projection.Editing.GetCurrentValue(variableId);

    void IEditableFormState.SetValue(VariableId variableId, string value)
        => _projection.Editing.SetValue(variableId, value);

    string? IReadableFormStatus.CurrentFieldId => _projection.Navigation.CurrentFocusedFieldId?.Value;

    string? IReadableFormStatus.CurrentFieldGroupId => _projection.Status.CurrentFieldGroupId;

    int IReadableFormStatus.CurrentFieldIndex => _projection.Status.CurrentFieldIndex;

    int IReadableFormStatus.CurrentFieldGroupIndex => _projection.Status.CurrentFieldGroupIndex;

    int IReadableFormStatus.TotalFields => _projection.Status.TotalFields;

    int IReadableFormStatus.TotalFieldGroups => _projection.Status.TotalFieldGroups;

    int IReadableFormStatus.OpenRequiredCount => _projection.Status.OpenRequiredCount;

    int IReadableFormStatus.ValidationErrorCount => _projection.Status.ValidationErrorCount;

    ImmutableArray<string> IReadableFormSummary.OrderedFieldIds => _projection.Summary.OrderedFieldIds;

    string? IReadableFormSummary.GetFieldLabel(string fieldId)
        => _projection.Summary.GetFieldLabel(fieldId);

    string? IReadableFormSummary.GetFieldValue(string fieldId)
        => _projection.Summary.GetFieldValue(fieldId);
}



