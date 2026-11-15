using System.Text.Json;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft Zielarten von Bedingungen sowie referenzierte Feld- und Variablenkennungen.
/// </summary>
internal static class FormConstraintTargetValidator
{
    internal static void ValidateTypedTarget(
        ConstraintTargetRef target,
        string path,
        IReadOnlyDictionary<string, VariableDefinition> variablesById,
        IReadOnlyDictionary<string, FieldDefinition> fieldsById,
        List<PreflightIssue> errors,
        string constraintId)
    {
        ArgumentNullException.ThrowIfNull(target);

        switch (target.Kind)
        {
            case "field":
                ValidateTypedFieldTarget(target.FieldId, path, fieldsById, errors, constraintId);
                break;
            case "variable":
                ValidateTypedVariableTarget(target.VariableId, path, variablesById, errors, constraintId);
                break;
            case "form":
                break;
            default:
                errors.Add(new PreflightIssue(
                    Code: PreflightCodes.ErrorFeatureNotSupported,
                    Message: $"Unsupported constraint target kind '{target.Kind}'.",
                    Path: $"{path}.kind",
                    Id: constraintId));
                break;
        }
    }

    internal static void ValidateJsonTarget(
        JsonElement constraint,
        string path,
        IReadOnlyDictionary<string, VariableDefinition> variablesById,
        IReadOnlyDictionary<string, FieldDefinition> fieldsById,
        List<PreflightIssue> errors)
    {
        if (!constraint.TryGetProperty("target", out var target)
            || target.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        var targetKind = PreflightJsonValueReader.TryGetString(target, "kind");
        switch (targetKind)
        {
            case "field":
                ValidateJsonFieldTarget(target, path, fieldsById, errors);
                break;
            case "variable":
                ValidateJsonVariableTarget(target, path, variablesById, errors);
                break;
            case "form":
                break;
            default:
                errors.Add(new PreflightIssue(
                    Code: PreflightCodes.ErrorFeatureNotSupported,
                    Message: $"Unsupported constraint target kind '{targetKind ?? "<missing>"}'.",
                    Path: $"{path}.target.kind"));
                break;
        }
    }

    private static void ValidateTypedFieldTarget(
        string? fieldId,
        string path,
        IReadOnlyDictionary<string, FieldDefinition> fieldsById,
        List<PreflightIssue> errors,
        string constraintId)
    {
        if (!string.IsNullOrWhiteSpace(fieldId) && fieldsById.ContainsKey(fieldId))
        {
            return;
        }

        errors.Add(new PreflightIssue(
            Code: PreflightCodes.ErrorRefMissing,
            Message: $"Constraint target references missing fieldId '{fieldId}'.",
            Path: $"{path}.fieldId",
            Id: constraintId));
    }

    private static void ValidateTypedVariableTarget(
        string? variableId,
        string path,
        IReadOnlyDictionary<string, VariableDefinition> variablesById,
        List<PreflightIssue> errors,
        string constraintId)
    {
        if (!string.IsNullOrWhiteSpace(variableId) && variablesById.ContainsKey(variableId))
        {
            return;
        }

        errors.Add(new PreflightIssue(
            Code: PreflightCodes.ErrorRefMissing,
            Message: $"Constraint target references missing variableId '{variableId}'.",
            Path: $"{path}.variableId",
            Id: constraintId));
    }

    private static void ValidateJsonFieldTarget(
        JsonElement target,
        string path,
        IReadOnlyDictionary<string, FieldDefinition> fieldsById,
        List<PreflightIssue> errors)
    {
        var fieldId = PreflightJsonValueReader.TryGetString(target, "fieldId");
        if (!string.IsNullOrWhiteSpace(fieldId) && fieldsById.ContainsKey(fieldId))
        {
            return;
        }

        errors.Add(new PreflightIssue(
            Code: PreflightCodes.ErrorRefMissing,
            Message: $"Constraint references missing fieldId '{fieldId}'.",
            Path: $"{path}.target.fieldId",
            Id: fieldId));
    }

    private static void ValidateJsonVariableTarget(
        JsonElement target,
        string path,
        IReadOnlyDictionary<string, VariableDefinition> variablesById,
        List<PreflightIssue> errors)
    {
        var variableId = PreflightJsonValueReader.TryGetString(target, "variableId");
        if (!string.IsNullOrWhiteSpace(variableId) && variablesById.ContainsKey(variableId))
        {
            return;
        }

        errors.Add(new PreflightIssue(
            Code: PreflightCodes.ErrorRefMissing,
            Message: $"Constraint references missing variableId '{variableId}'.",
            Path: $"{path}.target.variableId",
            Id: variableId));
    }
}
