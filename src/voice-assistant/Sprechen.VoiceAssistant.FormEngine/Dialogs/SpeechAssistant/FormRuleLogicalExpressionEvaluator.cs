using System.Text.Json;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Wertet logische Verknüpfungen aus und beendet die Prüfung, sobald das Ergebnis feststeht.
/// </summary>
internal sealed class FormRuleLogicalExpressionEvaluator
{
    private readonly Func<JsonElement, bool> _evaluateExpression;

    public FormRuleLogicalExpressionEvaluator(Func<JsonElement, bool> evaluateExpression)
    {
        _evaluateExpression = evaluateExpression ?? throw new ArgumentNullException(nameof(evaluateExpression));
    }

    public bool EvaluateAnd(JsonElement expression)
    {
        if (!TryGetExpressionArray(expression, out var expressionsElement))
        {
            return false;
        }

        foreach (var item in expressionsElement.EnumerateArray())
        {
            if (!_evaluateExpression(item))
            {
                return false;
            }
        }

        return true;
    }

    public bool EvaluateOr(JsonElement expression)
    {
        if (!TryGetExpressionArray(expression, out var expressionsElement))
        {
            return false;
        }

        foreach (var item in expressionsElement.EnumerateArray())
        {
            if (_evaluateExpression(item))
            {
                return true;
            }
        }

        return false;
    }

    public bool EvaluateNot(JsonElement expression)
    {
        if (!expression.TryGetProperty("expression", out var inner))
        {
            return false;
        }

        return !_evaluateExpression(inner);
    }

    private static bool TryGetExpressionArray(JsonElement expression, out JsonElement expressionsElement)
    {
        return expression.TryGetProperty("expressions", out expressionsElement)
            && expressionsElement.ValueKind == JsonValueKind.Array;
    }
}
