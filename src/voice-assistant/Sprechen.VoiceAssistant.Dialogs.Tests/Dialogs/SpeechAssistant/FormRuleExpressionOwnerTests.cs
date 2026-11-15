using System.Text.Json;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormRuleExpressionOwnerTests
{
    [Fact]
    public void Evaluate_WhenLogicalExpressionContainsComparison_DelegatesToExpressionFamilyOwners()
    {
        var evaluator = CreateEvaluator(new Dictionary<string, FormValue>
        {
            ["country"] = FormValue.FromString("DE"),
            ["age"] = FormValue.FromInteger(18)
        });

        var expression = ParseJson("""
            {
                "kind": "and",
                "expressions": [
                    {
                        "kind": "equals",
                        "left": { "kind": "var", "variableId": "country" },
                        "right": { "kind": "const", "value": "DE" }
                    },
                    {
                        "kind": "compare",
                        "operator": "gte",
                        "left": { "kind": "var", "variableId": "age" },
                        "right": { "kind": "const", "value": 18 }
                    }
                ]
            }
            """);

        Assert.True(evaluator.Evaluate(expression));
    }

    [Fact]
    public void Evaluate_WhenExpressionKindIsUnknown_Throws()
    {
        var evaluator = CreateEvaluator();
        var expression = ParseJson("""{ "kind": "unknown" }""");

        Assert.Throws<InvalidOperationException>(() => evaluator.Evaluate(expression));
    }

    [Fact]
    public void TryEvaluate_WhenVariableExpressionHasBlankVariableId_ReturnsFalse()
    {
        var valueEvaluator = new FormRuleValueExpressionEvaluator(_ => FormValue.FromString("ignored"));
        var expression = ParseJson("""{ "kind": "var", "variableId": " " }""");

        Assert.False(valueEvaluator.TryEvaluate(expression, out var value));
        Assert.Null(value);
    }

    private static FormRuleExpressionEvaluator CreateEvaluator(Dictionary<string, FormValue>? values = null)
    {
        values ??= [];
        FormValue? GetVariableValue(string variableId)
            => values.TryGetValue(variableId, out var value) ? value : null;

        return new FormRuleExpressionEvaluator(
            new FormRuleValueExpressionEvaluator(GetVariableValue),
            GetVariableValue);
    }

    private static JsonElement ParseJson(string json)
        => JsonDocument.Parse(json).RootElement;
}
