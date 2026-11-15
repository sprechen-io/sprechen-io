using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.FormSurfaces;

namespace Sprechen.Contracts.FormSurfaces;

/// <summary>
/// Führt Beschriftung, Eingabetyp und Validierungsregeln eines Feldes zusammen, damit die Oberfläche eine gemeinsame Darstellungsvorgabe erhält.
/// </summary>
internal sealed class FormFieldProjector
{
    private readonly FormAccessibilityLabelSelector _accessibilityLabelSelector;
    private readonly FormOptionProjector _optionProjector;
    private readonly FormInputTypeMapper _inputTypeMapper;

    public FormFieldProjector(
        FormAccessibilityLabelSelector accessibilityLabelSelector,
        FormOptionProjector optionProjector,
        FormInputTypeMapper inputTypeMapper)
    {
        _accessibilityLabelSelector = accessibilityLabelSelector ?? throw new ArgumentNullException(nameof(accessibilityLabelSelector));
        _optionProjector = optionProjector ?? throw new ArgumentNullException(nameof(optionProjector));
        _inputTypeMapper = inputTypeMapper ?? throw new ArgumentNullException(nameof(inputTypeMapper));
    }

    public FormSurfaceField Project(
        FormContract contract,
        FieldDefinition field,
        string fieldGroupId,
        IReadOnlyDictionary<string, VariableDefinition> variablesById,
        FieldConstraints fieldConstraints,
        string requestedLanguage,
        PolitenessLevel politenessLevel)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(variablesById);
        ArgumentNullException.ThrowIfNull(fieldConstraints);
        if (field.FieldId == default)
        {
            throw new ArgumentException("FieldId must be provided.", nameof(field));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(fieldGroupId);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedLanguage);

        if (!Enum.IsDefined(politenessLevel))
        {
            throw new ArgumentOutOfRangeException(nameof(politenessLevel), politenessLevel, "Unknown politeness level.");
        }

        var variableId = field.VariableId == default ? string.Empty : field.VariableId.Value;
        var variable = ResolveVariable(variableId, variablesById);
        var valueTypeKind = variable is null ? string.Empty : _inputTypeMapper.TryGetValueTypeKind(variable.ValueType) ?? string.Empty;
        var options = _optionProjector.ProjectOptions(contract, valueTypeKind, variable, fieldConstraints, requestedLanguage, politenessLevel);
        var inputType = _inputTypeMapper.MapInputType(valueTypeKind, variable, field, fieldConstraints, options);

        return new FormSurfaceField(
            field.FieldId.Value,
            fieldGroupId,
            _accessibilityLabelSelector.SelectLabel(contract, field, requestedLanguage, politenessLevel),
            valueTypeKind,
            field.CodeStyle,
            inputType,
            fieldConstraints.Required,
            field.Editability == FieldEditability.UserEditable,
            _accessibilityLabelSelector.SelectHelpText(contract, field, requestedLanguage, politenessLevel),
            _accessibilityLabelSelector.SelectPlaceholder(contract, field, requestedLanguage, politenessLevel),
            autocomplete: field.Projections?.Html?.Autocomplete?.Trim(),
            inputMode: field.Projections?.Html?.InputMode?.Trim(),
            pattern: fieldConstraints.Pattern,
            minLength: fieldConstraints.MinLength,
            maxLength: fieldConstraints.MaxLength,
            min: fieldConstraints.Min,
            max: fieldConstraints.Max,
            options: options,
            examples: _accessibilityLabelSelector.SelectExamples(contract, field, requestedLanguage, politenessLevel),
            accessibleDescription: _accessibilityLabelSelector.SelectAccessibleDescription(contract, field, requestedLanguage, politenessLevel),
            variableId: variableId,
            isReadOnlyDisplay: field.Editability == FieldEditability.ReadOnly);
    }

    private static VariableDefinition? ResolveVariable(
        string variableId,
        IReadOnlyDictionary<string, VariableDefinition> variablesById)
    {
        if (string.IsNullOrWhiteSpace(variableId))
        {
            return null;
        }

        if (variablesById.TryGetValue(variableId, out var variable))
        {
            return variable;
        }

        throw new InvalidOperationException($"Form surface field references unknown variable '{variableId}'.");
    }
}
