namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Ordnet unterstützte ganzzahlige Vergleichsoperatoren ihrer Auswertung zu und trennt deren Erkennung vom jeweiligen Wahrheitswert.
/// </summary>
internal static class FormIntegerComparisonOperatorEvaluator
{
    public static bool TryEvaluate(string operatorValue, int left, int right, out bool result)
    {
        result = false;

        if (string.IsNullOrWhiteSpace(operatorValue))
        {
            return false;
        }

        var normalizedOperator = operatorValue.Trim();

        result = normalizedOperator switch
        {
            "eq" => left == right,
            "neq" => left != right,
            "gt" => left > right,
            "gte" => left >= right,
            "lt" => left < right,
            "lte" => left <= right,
            _ => false
        };

        return normalizedOperator is "eq" or "neq" or "gt" or "gte" or "lt" or "lte";
    }
}
