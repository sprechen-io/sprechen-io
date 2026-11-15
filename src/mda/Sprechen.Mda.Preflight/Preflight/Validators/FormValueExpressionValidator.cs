using System.Text.Json;
using Sprechen.Mda.Contracts.V1.FormContracts;
namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft unterstützte Wertausdrucksarten, warnt vor unbekannten angegebenen Variablen
/// und meldet explizite Nullwerte in Konstantenausdrücken.
/// </summary>
internal sealed class FormValueExpressionValidator
{
    private static readonly IReadOnlySet<string> SupportedValueExpressionKinds = ReferenceImplementationSupportProfile.SupportedValueExpressionKinds;

    public void Validate(
        JsonElement valueExpression,
        string path,
        IReadOnlyDictionary<string, VariableDefinition> variablesById,
        List<PreflightIssue> errors,
        List<PreflightIssue> warnings)
    {
        if (valueExpression.ValueKind != JsonValueKind.Object)
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorFeatureNotSupported,
                Message: "ValueExpression must be a JSON object.",
                Path: path));
            return;
        }

        var kind = PreflightJsonValueReader.TryGetString(valueExpression, "kind");
        if (kind is null || !SupportedValueExpressionKinds.Contains(kind))
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorFeatureNotSupported,
                Message: $"Unsupported value expression kind '{kind ?? "<missing>"}'.",
                Path: $"{path}.kind"));
            return;
        }

        ValidateKnownKind(valueExpression, path, kind, variablesById, errors, warnings);
    }

    private static void ValidateKnownKind(
        JsonElement valueExpression,
        string path,
        string kind,
        IReadOnlyDictionary<string, VariableDefinition> variablesById,
        List<PreflightIssue> errors,
        List<PreflightIssue> warnings)
    {
        switch (kind)
        {
            case "var":
                ValidateVariableValueExpression(valueExpression, path, variablesById, warnings);
                break;
            case "const":
                ValidateConstValueExpression(valueExpression, path, errors);
                break;
        }
    }

    private static void ValidateVariableValueExpression(
        JsonElement valueExpression,
        string path,
        IReadOnlyDictionary<string, VariableDefinition> variablesById,
        List<PreflightIssue> warnings)
    {
        var variableId = PreflightJsonValueReader.TryGetString(valueExpression, "variableId");
        if (!string.IsNullOrWhiteSpace(variableId) && !variablesById.ContainsKey(variableId))
        {
            warnings.Add(new PreflightIssue(
                Code: PreflightCodes.WarnExprVarUnknown,
                Message: $"ValueExpression references unknown variableId '{variableId}'.",
                Path: $"{path}.variableId",
                Id: variableId));
        }
    }

    private static void ValidateConstValueExpression(JsonElement valueExpression, string path, List<PreflightIssue> errors)
    {
        if (valueExpression.TryGetProperty("value", out var value) && value.ValueKind == JsonValueKind.Null)
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorNullForbidden,
                Message: "const.value must not be null.",
                Path: $"{path}.value"));
        }
    }
}
