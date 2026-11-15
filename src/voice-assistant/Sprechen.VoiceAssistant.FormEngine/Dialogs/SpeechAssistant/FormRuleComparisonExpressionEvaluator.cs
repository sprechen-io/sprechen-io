using System.Text.Json;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Wertet Gleichheit, Mitgliedschaft und Ganzzahlvergleiche für Formularregeln aus.
/// </summary>
internal sealed class FormRuleComparisonExpressionEvaluator
{
    private readonly FormRuleValueExpressionEvaluator _valueExpressionEvaluator;

    public FormRuleComparisonExpressionEvaluator(FormRuleValueExpressionEvaluator valueExpressionEvaluator)
    {
        _valueExpressionEvaluator = valueExpressionEvaluator ?? throw new ArgumentNullException(nameof(valueExpressionEvaluator));
    }

    public bool EvaluateEquals(JsonElement expression)
    {
        if (!TryGetBinaryValues(expression, out var leftValue, out var rightValue))
        {
            return false;
        }

        return ValuesEqual(leftValue, rightValue);
    }

    public bool EvaluateNotEquals(JsonElement expression)
    {
        if (!TryGetBinaryValues(expression, out var leftValue, out var rightValue))
        {
            return false;
        }

        return !ValuesEqual(leftValue, rightValue);
    }

    public bool EvaluateCompare(JsonElement expression)
    {
        if (!TryGetBinaryValues(expression, out var leftValue, out var rightValue)
            || !FormRuleJsonExpressionReader.TryGetStringProperty(expression, "operator", out var operatorValue))
        {
            return false;
        }

        if (leftValue.Kind != FormValueKind.Integer || rightValue.Kind != FormValueKind.Integer)
        {
            return false;
        }

        return FormIntegerComparisonOperatorEvaluator.TryEvaluate(operatorValue, leftValue.Integer, rightValue.Integer, out var result)
            && result;
    }

    public bool EvaluateIn(JsonElement expression)
    {
        if (!expression.TryGetProperty("value", out var valueElement)
            || !expression.TryGetProperty("set", out var setElement)
            || setElement.ValueKind != JsonValueKind.Array
            || !_valueExpressionEvaluator.TryEvaluate(valueElement, out var candidateValue)
            || candidateValue is null)
        {
            return false;
        }

        foreach (var item in setElement.EnumerateArray())
        {
            if (!_valueExpressionEvaluator.TryEvaluate(item, out var setValue) || setValue is null)
            {
                continue;
            }

            if (candidateValue.Value.Kind == FormValueKind.Set)
            {
                // Bei einem Mengenkandidaten genügt ein Element, das einem der skalaren
                // Zeichenketteneinträge entspricht. Der Vergleich verlangt keine vollständige Mengengleichheit.
                if (candidateValue.Value.Set.Any(element =>
                        setValue.Value.Kind == FormValueKind.String
                        && string.Equals(element, setValue.Value.String, StringComparison.Ordinal)))
                {
                    return true;
                }

                continue;
            }

            if (Equals(candidateValue.Value, setValue.Value))
            {
                return true;
            }
        }

        return false;
    }

    // Ganze Mengen anhand ihrer kanonisch geordneten Einträge vergleichen.
    // Dieser Pfad setzt deren bereits erfolgte Normalisierung voraus. Ein Vergleich
    // der Array-Hüllen selbst würde stattdessen ihre Referenzen berücksichtigen.
    private static bool ValuesEqual(FormValue left, FormValue right)
    {
        if (left.Kind == FormValueKind.Set && right.Kind == FormValueKind.Set)
        {
            return left.Set.SequenceEqual(right.Set);
        }

        return Equals(left, right);
    }

    private bool TryGetBinaryValues(JsonElement expression, out FormValue leftValue, out FormValue rightValue)
    {
        leftValue = default;
        rightValue = default;

        if (!expression.TryGetProperty("left", out var leftElement)
            || !expression.TryGetProperty("right", out var rightElement)
            || !_valueExpressionEvaluator.TryEvaluate(leftElement, out FormValue? resolvedLeftValue)
            || !_valueExpressionEvaluator.TryEvaluate(rightElement, out FormValue? resolvedRightValue)
            || resolvedLeftValue is null
            || resolvedRightValue is null)
        {
            return false;
        }

        leftValue = resolvedLeftValue.Value;
        rightValue = resolvedRightValue.Value;
        return true;
    }
}
