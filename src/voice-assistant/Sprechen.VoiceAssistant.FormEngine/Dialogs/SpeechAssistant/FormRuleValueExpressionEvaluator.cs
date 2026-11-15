using System.Text.Json;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Löst Variablen und unterstützte Konstanten einschließlich Zeichenkettenmengen in Regelwerte auf, damit Vergleiche typisierte Operanden erhalten.
/// </summary>
internal sealed class FormRuleValueExpressionEvaluator
{
    private readonly Func<string, FormValue?> _getVariableValue;

    public FormRuleValueExpressionEvaluator(Func<string, FormValue?> getVariableValue)
    {
        _getVariableValue = getVariableValue ?? throw new ArgumentNullException(nameof(getVariableValue));
    }

    public bool TryEvaluate(JsonElement expression, out FormValue? value)
    {
        value = null;

        if (!FormRuleJsonExpressionReader.TryGetStringProperty(expression, "kind", out var kind))
        {
            return false;
        }

        if (string.Equals(kind, ValueExpressionKinds.Var, StringComparison.OrdinalIgnoreCase))
        {
            return TryEvaluateVariable(expression, out value);
        }

        if (string.Equals(kind, ValueExpressionKinds.Const, StringComparison.OrdinalIgnoreCase))
        {
            return TryEvaluateConstant(expression, out value);
        }

        return false;
    }

    private bool TryEvaluateVariable(JsonElement expression, out FormValue? value)
    {
        value = null;

        if (!FormRuleJsonExpressionReader.TryGetExactStringProperty(expression, "variableId", out var variableId))
        {
            return false;
        }

        value = _getVariableValue(variableId);
        return true;
    }

    private static bool TryEvaluateConstant(JsonElement expression, out FormValue? value)
    {
        value = null;

        if (!expression.TryGetProperty("value", out var raw))
        {
            return false;
        }

        if (FormRuleConstValueParser.TryParse(raw, out var constantValue))
        {
            value = constantValue;
            return true;
        }

        // Nicht leere Zeichenkettenarrays als Mengenkonstanten lesen. Dieser Ausdruckspfad
        // ergänzt den gemeinsamen Parser für skalare Konstanten.
        if (raw.ValueKind == JsonValueKind.Array)
        {
            var items = new List<string>();
            var valid = true;
            foreach (var item in raw.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(item.GetString()))
                {
                    valid = false;
                    break;
                }

                items.Add(item.GetString()!);
            }

            if (valid && items.Count > 0)
            {
                value = FormValue.FromSet([.. items]);
                return true;
            }
        }

        return false;
    }
}
