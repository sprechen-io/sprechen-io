using System.Text.Json;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;
using Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft Gleichheit und Ungleichheit vollständiger Mengen unabhängig von der Reihenfolge.
/// Bei in genügt für einen Mengenwert ein passendes Element. Weitere Beispiele
/// prüfen skalare Werte.
/// </summary>
public sealed class FormRuleComparisonSetEvaluatorTests
{
    private static FormRuleComparisonExpressionEvaluator CreateEvaluator(IReadOnlyDictionary<string, FormValue> variables)
        => new(new FormRuleValueExpressionEvaluator(id => variables.TryGetValue(id, out var value) ? value : null));

    private static JsonElement Expr(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private static readonly IReadOnlyDictionary<string, FormValue> Vars = new Dictionary<string, FormValue>
    {
        ["channels"] = FormValue.FromSet(System.Collections.Immutable.ImmutableArray.Create("phone", "email")),
        ["color"] = FormValue.FromString("gruen")
    };

    [Fact]
    public void RuleEquals_SetVarEqualsConstSet_DifferentOrder_Passes()
    {
        var evaluator = CreateEvaluator(Vars);
        var expression = Expr(@"{ ""kind"": ""equals"", ""left"": { ""kind"": ""var"", ""variableId"": ""channels"" }, ""right"": { ""kind"": ""const"", ""value"": [""email"", ""phone""] } }");
        Assert.True(evaluator.EvaluateEquals(expression));
    }

    [Fact]
    public void RuleEquals_SetVarSubsetOfConst_Fails()
    {
        var variables = new Dictionary<string, FormValue> { ["channels"] = FormValue.FromSet(System.Collections.Immutable.ImmutableArray.Create("phone")) };
        var evaluator = CreateEvaluator(variables);
        var expression = Expr(@"{ ""kind"": ""equals"", ""left"": { ""kind"": ""var"", ""variableId"": ""channels"" }, ""right"": { ""kind"": ""const"", ""value"": [""email"", ""phone""] } }");
        Assert.False(evaluator.EvaluateEquals(expression));
    }

    [Fact]
    public void RuleNotEquals_DifferentSet_Passes()
    {
        var variables = new Dictionary<string, FormValue> { ["channels"] = FormValue.FromSet(System.Collections.Immutable.ImmutableArray.Create("letter")) };
        var evaluator = CreateEvaluator(variables);
        var expression = Expr(@"{ ""kind"": ""notEquals"", ""left"": { ""kind"": ""var"", ""variableId"": ""channels"" }, ""right"": { ""kind"": ""const"", ""value"": [""email""] } }");
        Assert.True(evaluator.EvaluateNotEquals(expression));
    }

    [Fact]
    public void RuleIn_ScalarCandidateInList_Passes()
    {
        var evaluator = CreateEvaluator(Vars);
        var expression = Expr(@"{ ""kind"": ""in"", ""value"": { ""kind"": ""var"", ""variableId"": ""color"" }, ""set"": [ { ""kind"": ""const"", ""value"": ""gruen"" }, { ""kind"": ""const"", ""value"": ""rot"" } ] }");
        Assert.True(evaluator.EvaluateIn(expression));
    }

    [Fact]
    public void RuleIn_SetCandidateAnyElementMatches_Passes()
    {
        // channels enthält phone und email; das gemeinsame Element email genügt für in.
        var evaluator = CreateEvaluator(Vars);
        var expression = Expr(@"{ ""kind"": ""in"", ""value"": { ""kind"": ""var"", ""variableId"": ""channels"" }, ""set"": [ { ""kind"": ""const"", ""value"": ""email"" }, { ""kind"": ""const"", ""value"": ""letter"" } ] }");
        Assert.True(evaluator.EvaluateIn(expression));
    }

    [Fact]
    public void RuleIn_SetCandidateNoElementMatches_Fails()
    {
        var evaluator = CreateEvaluator(Vars);
        var expression = Expr(@"{ ""kind"": ""in"", ""value"": { ""kind"": ""var"", ""variableId"": ""channels"" }, ""set"": [ { ""kind"": ""const"", ""value"": ""letter"" }, { ""kind"": ""const"", ""value"": ""fax"" } ] }");
        Assert.False(evaluator.EvaluateIn(expression));
    }

    [Fact]
    public void RuleEquals_ScalarVarEqualsConst_Passes()
    {
        var evaluator = CreateEvaluator(Vars);
        var expression = Expr(@"{ ""kind"": ""equals"", ""left"": { ""kind"": ""var"", ""variableId"": ""color"" }, ""right"": { ""kind"": ""const"", ""value"": ""gruen"" } }");
        Assert.True(evaluator.EvaluateEquals(expression));
    }
}
