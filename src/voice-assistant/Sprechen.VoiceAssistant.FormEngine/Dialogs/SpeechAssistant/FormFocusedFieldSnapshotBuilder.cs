using System.Collections.Immutable;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Erstellt eine Momentaufnahme des fokussierten Felds für Dialogaufrufe.
/// TryBuild liefert null, wenn der Fokus oder seine Felddefinition fehlt.
/// </summary>
internal sealed class FormFocusedFieldSnapshotBuilder
{
    private readonly FormDialogDefinitionState _definitionState;
    private readonly FormMutableStateStore _mutableState;
    private readonly FormFieldInputModeAvailabilityPolicy _inputModeAvailabilityPolicy;
    private readonly FormPrivacyReadbackPolicy _privacyReadbackService;

    public FormFocusedFieldSnapshotBuilder(
        FormDialogDefinitionState definitionState,
        FormMutableStateStore mutableState,
        FormFieldInputModeAvailabilityPolicy inputModeAvailabilityPolicy,
        FormPrivacyReadbackPolicy privacyReadbackService)
    {
        _definitionState = definitionState ?? throw new ArgumentNullException(nameof(definitionState));
        _mutableState = mutableState ?? throw new ArgumentNullException(nameof(mutableState));
        _inputModeAvailabilityPolicy = inputModeAvailabilityPolicy ?? throw new ArgumentNullException(nameof(inputModeAvailabilityPolicy));
        _privacyReadbackService = privacyReadbackService ?? throw new ArgumentNullException(nameof(privacyReadbackService));
    }

    public FormFieldSnapshot Build()
        => TryBuild() ?? throw new InvalidOperationException("No focused field available.");

    public FormFieldSnapshot? TryBuild()
    {
        if (_mutableState.FocusedFieldId is not { } fieldId || !_definitionState.FieldsById.TryGetValue(fieldId, out var field))
        {
            return null;
        }

        var fieldGroupId = _definitionState.FieldToFieldGroupId[fieldId];
        var variableId = _definitionState.FieldToVariableId[fieldId];
        var valueTypeKind = FormVariableValueReader.TryGetValueTypeKind(_definitionState.VariablesById, variableId)
            ?? ValueTypeKinds.String;

        return new FormFieldSnapshot(
            new FormFieldIdentitySnapshot(
                fieldId,
                variableId,
                fieldGroupId),
            new FormFieldPresentationSnapshot(
                _definitionState.LocalizationResolver.ResolveFieldGroupTitle(fieldGroupId),
                _definitionState.LocalizationResolver.ResolveText(field.Label, fieldId),
                field.HelpText is null ? null : _definitionState.LocalizationResolver.ResolveText(field.HelpText, fallback: null),
                ResolveExamples(field),
                ResolveOptions(variableId, valueTypeKind)),
            BuildInteractionSnapshot(field, fieldId, variableId, valueTypeKind));
    }

    private FormFieldInteractionSnapshot BuildInteractionSnapshot(
        FieldDefinition field,
        FieldId fieldId,
        string variableId,
        string valueTypeKind)
    {
        var (minSelected, maxSelected) = valueTypeKind is ValueTypeKinds.Set
            ? FormVariableValueReader.GetSetSelectionBounds(_definitionState.VariablesById, variableId)
            : (null, null);

        var (_, variable) = _definitionState.ResolveFieldVariable(fieldId);
        return new FormFieldInteractionSnapshot(
            field.CodeStyle,
            _inputModeAvailabilityPolicy.ResolveInputMode(field, variable, fieldId),
            field.Editability == FieldEditability.Unknown ? EditabilityModes.UserEditable : field.Editability.ToWireToken(),
            valueTypeKind,
            _privacyReadbackService.CanSpeakValueInConfirmation(variableId, explicitOptIn: false))
        {
            MinSelected = minSelected,
            MaxSelected = maxSelected
        };
    }

    private ImmutableArray<string> ResolveExamples(FieldDefinition field)
    {
        if (field.Examples is null)
        {
            return [];
        }

        return field.Examples
            .Select(example => _definitionState.LocalizationResolver.ResolveText(example, fallback: null))
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .Select(text => text.Trim())
            .ToImmutableArray();
    }

    private ImmutableArray<FormChoiceOption> ResolveOptions(string variableId, string valueTypeKind)
        => valueTypeKind is ValueTypeKinds.Enum or ValueTypeKinds.Set
            ? _definitionState.LocalizationResolver.ResolveChoiceOptions(variableId)
            : [];
}
