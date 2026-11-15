using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Verbindet Definition und Zustand mit nachträglich gebundenen Navigationsdiensten. Nur dienstabhängige Zugriffe benötigen diese Bindung.
/// </summary>
internal sealed class FormDialogNavigationState : IFormActionNavigationState, IFormNavigatorState
{
    private readonly FormDialogDefinitionState _definitionState;
    private readonly FormMutableStateStore _mutableState;
    private FormDialogDependencies? _services;

    public FormDialogNavigationState(
        FormDialogDefinitionState definitionState,
        FormMutableStateStore mutableState)
    {
        _definitionState = definitionState ?? throw new ArgumentNullException(nameof(definitionState));
        _mutableState = mutableState ?? throw new ArgumentNullException(nameof(mutableState));
    }

    internal void Bind(FormDialogDependencies services)
    {
        if (_services is not null)
        {
            throw new InvalidOperationException("Form interpreter navigation state is already bound.");
        }

        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    private FormDialogDependencies Services
        => _services ?? throw new InvalidOperationException("Form interpreter navigation state is not bound.");

    bool IFormActionNavigationState.TryNavigateToFieldGroup(FieldGroupId fieldGroupId)
        => Services.Navigator.TryNavigateToFieldGroup(fieldGroupId);

    bool IFormActionNavigationState.TrySetFocus(FieldId fieldId)
        => Services.Navigator.TrySetFocus(fieldId);

    FieldId? IFormNavigatorState.CurrentFocusedFieldId
        => _mutableState.FocusedFieldId;

    ImmutableArray<string> IFormNavigatorState.OrderedFieldIds => _definitionState.OrderedFieldIds;

    void IFormNavigatorState.SetCurrentFocus(FieldId fieldId) => _mutableState.SetFocusedField(fieldId);

    bool IFormNavigatorState.TryGetFieldIndex(FieldId fieldId, out int index)
        => _definitionState.FieldIndexById.TryGetValue(fieldId, out index);

    bool IFormNavigatorState.IsFieldInputAvailable(FieldId fieldId)
        => Services.VisibilityService.IsFieldInputAvailable(fieldId);

    bool IFormNavigatorState.IsFieldAudiblyAvailable(FieldId fieldId)
        => Services.VisibilityService.IsFieldAudiblyAvailable(fieldId);

    bool IFormNavigatorState.IsKnownField(FieldId fieldId)
        => _definitionState.FieldsById.ContainsKey(fieldId);

    bool IFormNavigatorState.IsKnownFieldGroup(FieldGroupId fieldGroupId)
        => _definitionState.FieldGroupsById.ContainsKey(fieldGroupId);

    FieldGroupId? IFormNavigatorState.GetFieldGroupId(FieldId fieldId)
        => _definitionState.FieldToFieldGroupId.TryGetValue(fieldId, out var fieldGroup) ? FieldGroupId.From(fieldGroup) : null;

    bool IFormNavigatorState.IsFieldGroupOnPath(FieldGroupId fieldGroupId, FieldGroupId targetFieldGroupId)
        => Services.VisibilityService.IsFieldGroupOnPath(fieldGroupId, targetFieldGroupId);
}
