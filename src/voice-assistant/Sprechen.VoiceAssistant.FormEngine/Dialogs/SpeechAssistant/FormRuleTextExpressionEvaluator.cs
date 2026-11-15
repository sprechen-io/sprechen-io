using System.Text.Json;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft, ob ein Text einen Teiltext enthält oder mit einem Präfix beginnt.
/// Die Vergleiche ignorieren Groß- und Kleinschreibung.
/// </summary>
internal sealed class FormRuleTextExpressionEvaluator
{
    private readonly FormRuleValueExpressionEvaluator _valueExpressionEvaluator;

    public FormRuleTextExpressionEvaluator(FormRuleValueExpressionEvaluator valueExpressionEvaluator)
    {
        _valueExpressionEvaluator = valueExpressionEvaluator ?? throw new ArgumentNullException(nameof(valueExpressionEvaluator));
    }

    public bool EvaluateContains(JsonElement expression)
    {
        if (!TryGetStringOperand(expression, "text", out var text)
            || !TryGetStringOperand(expression, "substring", out var substring))
        {
            return false;
        }

        return text.Contains(substring, StringComparison.OrdinalIgnoreCase);
    }

    public bool EvaluateStartsWith(JsonElement expression)
    {
        if (!TryGetStringOperand(expression, "text", out var text)
            || !TryGetStringOperand(expression, "prefix", out var prefix))
        {
            return false;
        }

        return text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    private bool TryGetStringOperand(JsonElement expression, string propertyName, out string value)
    {
        value = string.Empty;

        if (!expression.TryGetProperty(propertyName, out var operandElement)
            || !_valueExpressionEvaluator.TryEvaluate(operandElement, out var operandValue)
            || operandValue is null
            || operandValue.Value.Kind != FormValueKind.String)
        {
            return false;
        }

        value = operandValue.Value.String;
        return true;
    }
}
