using Sprechen.Mda.Contracts.V1.Fields;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft Eingabemodi und Bedingungsarten wiederverwendbarer Felder
/// sowie die Bindung ihrer Bedingungen an die eigene Felddefinition.
/// </summary>
internal sealed class ReusableFieldSupportedSubsetValidator
{
    private static readonly IReadOnlySet<string> SupportedInputModes = ReferenceImplementationSupportProfile.SupportedInputModes;

    private static readonly IReadOnlySet<string> SupportedConstraintKinds = ReferenceImplementationSupportProfile.SupportedReusableFieldConstraintKinds;

    public void Validate(FieldDefinitions catalog, List<PreflightIssue> errors)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(errors);

        for (var i = 0; i < catalog.Definitions.Count; i++)
        {
            var definition = catalog.Definitions[i];
            ValidatePreferredInputModes(definition, i, errors);
            ValidateFieldConstraints(definition, i, errors);
        }
    }

    private static void ValidatePreferredInputModes(
        ReusableFieldDefinition definition,
        int definitionIndex,
        List<PreflightIssue> errors)
    {
        if (definition.PreferredInputModes is null)
        {
            return;
        }

        for (var j = 0; j < definition.PreferredInputModes.Count; j++)
        {
            var inputMode = definition.PreferredInputModes[j];
            if (string.IsNullOrWhiteSpace(inputMode) || !SupportedInputModes.Contains(inputMode))
            {
                errors.Add(new PreflightIssue(
                    Code: PreflightCodes.ErrorFeatureNotSupported,
                    Message: $"Unsupported preferredInputMode '{inputMode}' in reusable field '{definition.FieldDefinitionId}'.",
                    Path: $"$.definitions[{definitionIndex}].preferredInputModes[{j}]",
                    Id: definition.FieldDefinitionId));
            }
        }
    }

    private static void ValidateFieldConstraints(
        ReusableFieldDefinition definition,
        int definitionIndex,
        List<PreflightIssue> errors)
    {
        if (definition.FieldConstraints is null)
        {
            return;
        }

        for (var j = 0; j < definition.FieldConstraints.Count; j++)
        {
            var constraint = definition.FieldConstraints[j];
            ValidateConstraintKind(constraint, definition, definitionIndex, j, errors);
            ValidateConstraintTarget(constraint, definition, definitionIndex, j, errors);
        }
    }

    private static void ValidateConstraintKind(
        ConstraintDefinition constraint,
        ReusableFieldDefinition definition,
        int definitionIndex,
        int constraintIndex,
        List<PreflightIssue> errors)
    {
        if (constraint.Kind != ConstraintKind.Unknown
            && SupportedConstraintKinds.Contains(constraint.Kind.ToWireToken()))
        {
            return;
        }

        errors.Add(new PreflightIssue(
            Code: PreflightCodes.ErrorFeatureNotSupported,
            Message: $"Unsupported constraint kind '{constraint.Kind}' in reusable field '{definition.FieldDefinitionId}'.",
            Path: $"$.definitions[{definitionIndex}].fieldConstraints[{constraintIndex}].kind",
            Id: constraint.ConstraintId));
    }

    private static void ValidateConstraintTarget(
        ConstraintDefinition constraint,
        ReusableFieldDefinition definition,
        int definitionIndex,
        int constraintIndex,
        List<PreflightIssue> errors)
    {
        if (string.Equals(constraint.Target.Kind, ConstraintScopes.Field, StringComparison.Ordinal)
            && string.Equals(constraint.Target.FieldId, definition.FieldDefinitionId, StringComparison.Ordinal))
        {
            return;
        }

        errors.Add(new PreflightIssue(
            Code: PreflightCodes.ErrorFeatureNotSupported,
            Message: $"Reusable field constraints must target their own fieldDefinitionId '{definition.FieldDefinitionId}'.",
            Path: $"$.definitions[{definitionIndex}].fieldConstraints[{constraintIndex}].target",
            Id: constraint.ConstraintId));
    }
}
