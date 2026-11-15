using System.Text.Json;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft Vorhandensein und Leerheit aufgelöster Variablenwerte für die entsprechenden booleschen Regelausdrücke.
/// </summary>
internal sealed class FormRuleVariableStateExpressionEvaluator
{
    private readonly Func<string, FormValue?> _getVariableValue;

    public FormRuleVariableStateExpressionEvaluator(Func<string, FormValue?> getVariableValue)
    {
        _getVariableValue = getVariableValue ?? throw new ArgumentNullException(nameof(getVariableValue));
    }

    public bool EvaluateIsSet(JsonElement expression)
    {
        if (!FormRuleJsonExpressionReader.TryGetExactStringProperty(expression, "variableId", out var variableId))
        {
            return false;
        }

        return _getVariableValue(variableId) is not null;
    }

    public bool EvaluateIsNotSet(JsonElement expression)
    {
        if (!TryGetIsNotSetVariableId(expression, out var variableId))
        {
            return true;
        }

        return _getVariableValue(variableId) is null;
    }

    public bool EvaluateIsEmpty(JsonElement expression)
    {
        if (!FormRuleJsonExpressionReader.TryGetExactStringProperty(expression, "variableId", out var variableId))
        {
            return false;
        }

        var value = _getVariableValue(variableId);
        if (value is null)
        {
            return true;
        }

        return value.Value.Kind switch
        {
            FormValueKind.String => string.IsNullOrWhiteSpace(value.Value.String),
            FormValueKind.Set => value.Value.Set.Length == 0,
            _ => false
        };
    }

    public bool EvaluateIsNotEmpty(JsonElement expression)
        => !EvaluateIsEmpty(expression);

    private static bool TryGetIsNotSetVariableId(JsonElement expression, out string variableId)
    {
        variableId = string.Empty;

        return FormRuleJsonExpressionReader.TryGetExactStringProperty(expression, "variableId", out variableId);
    }
}
