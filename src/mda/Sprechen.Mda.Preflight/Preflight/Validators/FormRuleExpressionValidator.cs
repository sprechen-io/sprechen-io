using System.Text.Json;
using Sprechen.Mda.Contracts.V1.FormContracts;
namespace Sprechen.Mda.Contracts.V1.Preflight;

/// <summary>
/// Prüft unterstützte Ausdrucksarten und vorhandene Unterausdrücke
/// sowie deren Wert- und Variablenreferenzen.
/// </summary>
internal sealed class FormRuleExpressionValidator
{
    private static readonly IReadOnlySet<string> SupportedExpressionKinds = ReferenceImplementationSupportProfile.SupportedExpressionKinds;
    private readonly FormRuleActionValidator _actionValidator = new();
    private readonly FormValueExpressionValidator _valueExpressionValidator = new();
    private readonly FormVariableStateExpressionValidator _variableStateExpressionValidator = new();

    internal void ValidateAction(
        JsonElement action,
        string path,
        FormContractPreflightIndex index,
        List<PreflightIssue> errors)
    {
        _actionValidator.Validate(action, path, index, errors);
    }

    internal void ValidateExpressionUnion(
        JsonElement expression,
        string path,
        IReadOnlyDictionary<string, VariableDefinition> variablesById,
        List<PreflightIssue> errors,
        List<PreflightIssue> warnings)
    {
        if (expression.ValueKind != JsonValueKind.Object)
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorFeatureNotSupported,
                Message: "Expression must be a JSON object.",
                Path: path));
            return;
        }

        var kind = PreflightJsonValueReader.TryGetString(expression, "kind");
        if (kind is null || !SupportedExpressionKinds.Contains(kind))
        {
            errors.Add(new PreflightIssue(
                Code: PreflightCodes.ErrorFeatureNotSupported,
                Message: $"Unsupported expression kind '{kind ?? "<missing>"}'.",
                Path: $"{path}.kind"));
            return;
        }

        ValidateSupportedExpression(kind, expression, path, variablesById, errors, warnings);
    }

    private void ValidateSupportedExpression(
        string kind,
        JsonElement expression,
        string path,
        IReadOnlyDictionary<string, VariableDefinition> variablesById,
        List<PreflightIssue> errors,
        List<PreflightIssue> warnings)
    {
        switch (kind)
        {
            case ExpressionKinds.And:
            case ExpressionKinds.Or:
                ValidateNestedExpressionArray(expression, path, variablesById, errors, warnings);
                break;
            case ExpressionKinds.Not:
                ValidateNestedExpression(expression, path, variablesById, errors, warnings);
                break;
            case ExpressionKinds.Equals:
            case ExpressionKinds.NotEquals:
            case ExpressionKinds.Compare:
                ValidateValueExpressionProperties(expression, path, variablesById, errors, warnings, "left", "right");
                break;
            case ExpressionKinds.In:
                ValidateInExpression(expression, path, variablesById, errors, warnings);
                break;
            case ExpressionKinds.Contains:
                ValidateValueExpressionProperties(expression, path, variablesById, errors, warnings, "text", "substring");
                break;
            case ExpressionKinds.StartsWith:
                ValidateValueExpressionProperties(expression, path, variablesById, errors, warnings, "text", "prefix");
                break;
            case ExpressionKinds.IsSet:
            case ExpressionKinds.IsNotSet:
            case ExpressionKinds.IsEmpty:
            case ExpressionKinds.IsNotEmpty:
                _variableStateExpressionValidator.Validate(expression, path, variablesById, warnings);
                break;
        }
    }

    private void ValidateNestedExpressionArray(
        JsonElement expression,
        string path,
        IReadOnlyDictionary<string, VariableDefinition> variablesById,
        List<PreflightIssue> errors,
        List<PreflightIssue> warnings)
    {
        if (!expression.TryGetProperty("expressions", out var expressions) || expressions.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var index = 0;
        foreach (var item in expressions.EnumerateArray())
        {
            ValidateExpressionUnion(item, $"{path}.expressions[{index}]", variablesById, errors, warnings);
            index++;
        }
    }

    private void ValidateNestedExpression(
        JsonElement expression,
        string path,
        IReadOnlyDictionary<string, VariableDefinition> variablesById,
        List<PreflightIssue> errors,
        List<PreflightIssue> warnings)
    {
        if (expression.TryGetProperty("expression", out var inner))
        {
            ValidateExpressionUnion(inner, $"{path}.expression", variablesById, errors, warnings);
        }
    }

    private void ValidateValueExpressionProperties(
        JsonElement expression,
        string path,
        IReadOnlyDictionary<string, VariableDefinition> variablesById,
        List<PreflightIssue> errors,
        List<PreflightIssue> warnings,
        params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (expression.TryGetProperty(propertyName, out var property))
            {
                _valueExpressionValidator.Validate(property, $"{path}.{propertyName}", variablesById, errors, warnings);
            }
        }
    }

    private void ValidateInExpression(
        JsonElement expression,
        string path,
        IReadOnlyDictionary<string, VariableDefinition> variablesById,
        List<PreflightIssue> errors,
        List<PreflightIssue> warnings)
    {
        if (expression.TryGetProperty("value", out var value))
        {
            _valueExpressionValidator.Validate(value, $"{path}.value", variablesById, errors, warnings);
        }

        if (!expression.TryGetProperty("set", out var set) || set.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var index = 0;
        foreach (var item in set.EnumerateArray())
        {
            _valueExpressionValidator.Validate(item, $"{path}.set[{index}]", variablesById, errors, warnings);
            index++;
        }
    }

}
