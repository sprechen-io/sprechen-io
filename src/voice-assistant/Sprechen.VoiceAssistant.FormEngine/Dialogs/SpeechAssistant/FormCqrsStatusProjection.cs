using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Stellt Fokusposition, Feld- und Gruppenanzahl sowie die Anzahl offener Pflichtfelder und Validierungsfehler für Statusabfragen bereit.
/// </summary>
internal sealed class FormCqrsStatusProjection(
    FormDialogDefinitionState definitionState,
    FormMutableStateStore mutableState,
    FormFieldVisibilityPolicy visibilityService,
    FormStatusProjectionBuilder statusProjectionService,
    FormDialogValidator validationService)
{
    private readonly FormDialogDefinitionState _definitionState =
        definitionState ?? throw new ArgumentNullException(nameof(definitionState));
    private readonly FormMutableStateStore _mutableState =
        mutableState ?? throw new ArgumentNullException(nameof(mutableState));
    private readonly FormFieldVisibilityPolicy _visibilityService =
        visibilityService ?? throw new ArgumentNullException(nameof(visibilityService));
    private readonly FormStatusProjectionBuilder _statusProjectionService =
        statusProjectionService ?? throw new ArgumentNullException(nameof(statusProjectionService));
    private readonly FormDialogValidator _validationService =
        validationService ?? throw new ArgumentNullException(nameof(validationService));

    internal string? CurrentFieldGroupId => TryGetCurrentFieldGroupId();

    internal int CurrentFieldIndex
        => _mutableState.FocusedFieldId is { } focusedFieldId
            && _definitionState.FieldIndexById.TryGetValue(focusedFieldId, out var index)
            ? index + 1
            : 0;

    internal int CurrentFieldGroupIndex
    {
        get
        {
            var fieldGroupId = TryGetCurrentFieldGroupId();
            return fieldGroupId is not null && _definitionState.FieldGroupIndexById.TryGetValue(FieldGroupId.From(fieldGroupId), out var index)
                ? index + 1
                : 0;
        }
    }

    internal int TotalFields => _definitionState.OrderedFieldIds.Length;

    internal int TotalFieldGroups => _definitionState.OrderedFieldGroupIds.Length;

    internal int OpenRequiredCount
    {
        get
        {
            var violations = _validationService.ValidateAll(_visibilityService.IsFieldEffectivelyVisible);
            return _statusProjectionService.CountOpenRequiredFields(
                _mutableState.ValuesByVariableId,
                violations,
                _visibilityService.IsFieldEffectivelyVisible);
        }
    }

    internal int ValidationErrorCount
        => _validationService.ValidateAll(_visibilityService.IsFieldEffectivelyVisible).Count(violation =>
            string.Equals(violation.Severity, SeverityLevels.Error, StringComparison.OrdinalIgnoreCase));

    private string? TryGetCurrentFieldGroupId()
    {
        return _mutableState.FocusedFieldId is { } focusedFieldId
            && _definitionState.FieldToFieldGroupId.TryGetValue(focusedFieldId, out var fieldGroupId)
            ? fieldGroupId
            : null;
    }
}
