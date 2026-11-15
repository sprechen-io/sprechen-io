using System.Text.Json;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet unterstützte boolesche Ausdrucksarten ihren Auswertern zu und weist unbekannte benannte Arten zurück, statt ihnen einen Wahrheitswert zu unterstellen.
/// </summary>
internal sealed class FormRuleExpressionEvaluator
{
    private readonly IReadOnlyDictionary<string, Func<JsonElement, bool>> _evaluators;

    public FormRuleExpressionEvaluator(
        FormRuleValueExpressionEvaluator valueExpressionEvaluator,
        Func<string, FormValue?> getVariableValue)
    {
        ArgumentNullException.ThrowIfNull(valueExpressionEvaluator);
        ArgumentNullException.ThrowIfNull(getVariableValue);

        var logicalEvaluator = new FormRuleLogicalExpressionEvaluator(Evaluate);
        var comparisonEvaluator = new FormRuleComparisonExpressionEvaluator(valueExpressionEvaluator);
        var textEvaluator = new FormRuleTextExpressionEvaluator(valueExpressionEvaluator);
        var variableStateEvaluator = new FormRuleVariableStateExpressionEvaluator(getVariableValue);

        _evaluators = new Dictionary<string, Func<JsonElement, bool>>(StringComparer.OrdinalIgnoreCase)
        {
            [ExpressionKinds.And] = logicalEvaluator.EvaluateAnd,
            [ExpressionKinds.Or] = logicalEvaluator.EvaluateOr,
            [ExpressionKinds.Not] = logicalEvaluator.EvaluateNot,
            [ExpressionKinds.Equals] = comparisonEvaluator.EvaluateEquals,
            [ExpressionKinds.NotEquals] = comparisonEvaluator.EvaluateNotEquals,
            [ExpressionKinds.Compare] = comparisonEvaluator.EvaluateCompare,
            [ExpressionKinds.In] = comparisonEvaluator.EvaluateIn,
            [ExpressionKinds.Contains] = textEvaluator.EvaluateContains,
            [ExpressionKinds.StartsWith] = textEvaluator.EvaluateStartsWith,
            [ExpressionKinds.IsSet] = variableStateEvaluator.EvaluateIsSet,
            [ExpressionKinds.IsNotSet] = variableStateEvaluator.EvaluateIsNotSet,
            [ExpressionKinds.IsEmpty] = variableStateEvaluator.EvaluateIsEmpty,
            [ExpressionKinds.IsNotEmpty] = variableStateEvaluator.EvaluateIsNotEmpty
        };
    }

    public bool Evaluate(JsonElement expression)
    {
        if (expression.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!FormRuleJsonExpressionReader.TryGetStringProperty(expression, "kind", out var kind))
        {
            return false;
        }

        if (!_evaluators.TryGetValue(kind, out var evaluator))
        {
            throw new InvalidOperationException($"Unsupported expression kind '{kind}'.");
        }

        return evaluator(expression);
    }
}
