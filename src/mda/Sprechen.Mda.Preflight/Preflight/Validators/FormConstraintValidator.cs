using System.Text.Json;
using Sprechen.Mda.Contracts.V1.FormContracts;
namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Koordiniert die unterstützten Prüfungen für typisierte und als JSON vorliegende
/// Formularbedingungen.
/// </summary>
internal sealed class FormConstraintValidator(FormRuleExpressionValidator ruleExpressionValidator)
{
    private readonly FormRuleExpressionValidator _ruleExpressionValidator = ruleExpressionValidator ?? throw new ArgumentNullException(nameof(ruleExpressionValidator));

    internal void Validate(
        ConstraintDefinition constraint,
        string path,
        IReadOnlyDictionary<string, VariableDefinition> variablesById,
        IReadOnlyDictionary<string, FieldDefinition> fieldsById,
        List<PreflightIssue> errors,
        List<PreflightIssue> warnings)
    {
        ArgumentNullException.ThrowIfNull(constraint);

        FormConstraintSupportValidator.ValidateTypedKindAndScope(constraint, path, errors);
        FormConstraintSupportValidator.WarnDuplicationProneTarget(constraint, path, warnings);
        FormConstraintTargetValidator.ValidateTypedTarget(constraint.Target, $"{path}.target", variablesById, fieldsById, errors, constraint.ConstraintId);

        if (constraint.When is not null)
        {
            _ruleExpressionValidator.ValidateExpressionUnion(constraint.When.Value, $"{path}.when", variablesById, errors, warnings);
        }

        FormConstraintPatternValidator.Validate(
            constraint.ConstraintId,
            constraint.Kind == ConstraintKind.Unknown ? null : constraint.Kind.ToWireToken(),
            PreflightJsonValueReader.TryGetConstraintStringProperty(constraint, "pattern"),
            path,
            errors);
        FormConstraintValueListValidator.ValidateTypedOneOfValues(constraint, path, errors);
    }

    internal void ValidateSupportedConstraint(JsonElement constraint, string path, List<PreflightIssue> errors)
    {
        if (constraint.ValueKind != JsonValueKind.Object)
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorFeatureNotSupported,
                Message: "Constraint must be a JSON object.",
                Path: path));
            return;
        }

        var kind = FormConstraintSupportValidator.ValidateJsonKind(constraint, path, errors);

        FormConstraintPatternValidator.Validate(
            PreflightJsonValueReader.TryGetString(constraint, "constraintId"),
            kind,
            PreflightJsonValueReader.TryGetString(constraint, "pattern"),
            path,
            errors);
        FormConstraintSupportValidator.ValidateJsonScope(constraint, path, errors);
        FormConstraintValueListValidator.ValidateJsonOneOfValues(constraint, kind, path, errors);
    }

    internal void Validate(
        JsonElement constraint,
        string path,
        IReadOnlyDictionary<string, VariableDefinition> variablesById,
        IReadOnlyDictionary<string, FieldDefinition> fieldsById,
        List<PreflightIssue> errors,
        List<PreflightIssue> warnings)
    {
        if (constraint.ValueKind != JsonValueKind.Object)
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorFeatureNotSupported,
                Message: "Constraint must be a JSON object.",
                Path: path));
            return;
        }

        var kind = FormConstraintSupportValidator.ValidateJsonKind(constraint, path, errors);
        FormConstraintSupportValidator.ValidateJsonScope(constraint, path, errors);
        FormConstraintTargetValidator.ValidateJsonTarget(constraint, path, variablesById, fieldsById, errors);

        if (constraint.TryGetProperty("when", out var whenElement) && whenElement.ValueKind != JsonValueKind.Null)
        {
            _ruleExpressionValidator.ValidateExpressionUnion(whenElement, $"{path}.when", variablesById, errors, warnings);
        }

        FormConstraintValueListValidator.ValidateJsonOneOfValues(constraint, kind, path, errors);
    }
}
