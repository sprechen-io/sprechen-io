using System.Text.Json;

namespace Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;

/// <summary>
/// Stellt die Auswertung modellierter Formularregeln über die gemeinsamen Ausdrucks-
/// und Wertauswerter bereit, damit Aufrufer deren Auflösung nicht selbst zusammensetzen.
/// </summary>
internal sealed class FormRuleEngine
{
    private readonly FormRuleExpressionEvaluator _expressionEvaluator;
    private readonly FormRuleValueExpressionEvaluator _valueExpressionEvaluator;

    public FormRuleEngine(Func<string, FormValue?> getVariableValue)
    {
        ArgumentNullException.ThrowIfNull(getVariableValue);

        _valueExpressionEvaluator = new FormRuleValueExpressionEvaluator(getVariableValue);
        _expressionEvaluator = new FormRuleExpressionEvaluator(_valueExpressionEvaluator, getVariableValue);
    }

    public bool EvaluateExpression(JsonElement expression)
        => _expressionEvaluator.Evaluate(expression);

    /// <summary>
    /// Löst einen Variablen- oder Konstantenausdruck in einen Formularwert auf.
    /// </summary>
    public bool TryEvaluateValueExpression(JsonElement expression, out FormValue? value)
        => _valueExpressionEvaluator.TryEvaluate(expression, out value);

    /// <summary>
    /// Versucht, einen skalaren JSON-Wert als Formularwertkonstante zu lesen.
    /// </summary>
    public static bool TryParseConstValue(JsonElement raw, out FormValue value)
        => FormRuleConstValueParser.TryParse(raw, out value);
}
