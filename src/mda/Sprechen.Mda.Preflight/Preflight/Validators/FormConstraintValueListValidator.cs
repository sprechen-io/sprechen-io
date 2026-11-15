using System.Text.Json;
using Sprechen.Mda.Contracts.V1.FormContracts;

namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Meldet Nullwerte in vorhandenen <c>oneOf.values</c>-Arrays.
/// </summary>
internal static class FormConstraintValueListValidator
{
    internal static void ValidateTypedOneOfValues(
        ConstraintDefinition constraint,
        string path,
        List<PreflightIssue> errors)
    {
        ArgumentNullException.ThrowIfNull(constraint);

        if (constraint.Kind != ConstraintKind.OneOf
            || constraint.AdditionalProperties is null
            || !constraint.AdditionalProperties.TryGetValue(ConstraintPropertyNames.Values, out var values)
            || values.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        ValidateOneOfValues(values, path, errors, constraint.ConstraintId);
    }

    internal static void ValidateJsonOneOfValues(
        JsonElement constraint,
        string? kind,
        string path,
        List<PreflightIssue> errors)
    {
        if (!string.Equals(kind, ConstraintKinds.OneOf, StringComparison.Ordinal)
            || !constraint.TryGetProperty(ConstraintPropertyNames.Values, out var values)
            || values.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        ValidateOneOfValues(values, path, errors);
    }

    private static void ValidateOneOfValues(
        JsonElement values,
        string path,
        List<PreflightIssue> errors,
        string? constraintId = null)
    {
        var index = 0;
        foreach (var item in values.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Null)
            {
                errors.Add(new PreflightIssue(
                    Code: PreflightCodes.ErrorNullForbidden,
                    Message: "oneOf.values must not contain null.",
                    Path: $"{path}.values[{index}]",
                    Id: constraintId));
            }

            index++;
        }
    }
}
