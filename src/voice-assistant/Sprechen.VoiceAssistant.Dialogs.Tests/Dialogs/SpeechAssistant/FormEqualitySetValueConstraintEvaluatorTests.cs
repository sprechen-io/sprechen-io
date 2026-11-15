using System.Text.Json;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;
using Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft Gleichheit und Ungleichheit vollständiger Mengen unabhängig von ihrer Reihenfolge.
/// Zusätzliche Fälle sichern den Vergleich einzelner Zeichenketten.
/// </summary>
public sealed class FormEqualitySetValueConstraintEvaluatorTests
{
    private static readonly FormEqualitySetValueConstraintEvaluator Evaluator = new();

    private static ConstraintDefinition Equality(string kind, string expectedJson)
    {
        using var document = JsonDocument.Parse($@"{{
            ""constraintId"": ""c1"",
            ""target"": {{ ""kind"": ""field"", ""fieldId"": ""channels"" }},
            ""severity"": ""error"",
            ""code"": ""test.equality"",
            ""kind"": ""{kind}"",
            ""value"": {expectedJson}
        }}");
        return JsonSerializer.Deserialize<ConstraintDefinition>(document.RootElement.GetRawText())!;
    }

    private static FormValue Set(params string[] values) => FormValue.FromSet(System.Collections.Immutable.ImmutableArray.Create(values));

    [Fact]
    public void Equals_SameSetDifferentOrder_Passes()
    {
        var constraint = Equality("equals", @"[""email"", ""phone""]");
        Assert.True(Evaluator.IsSatisfied("equals", constraint, Set("phone", "email")));
    }

    [Fact]
    public void Equals_Subset_Fails()
    {
        var constraint = Equality("equals", @"[""email"", ""phone""]");
        Assert.False(Evaluator.IsSatisfied("equals", constraint, Set("phone")));
    }

    [Fact]
    public void Equals_Superset_Fails()
    {
        var constraint = Equality("equals", @"""email""");
        Assert.False(Evaluator.IsSatisfied("equals", constraint, Set("email", "phone")));
    }

    [Fact]
    public void Equals_ScalarEntryActsAsSingleton_Passes()
    {
        var constraint = Equality("equals", @"""email""");
        Assert.True(Evaluator.IsSatisfied("equals", constraint, Set("email")));
    }

    [Fact]
    public void NotEquals_DifferentSet_Passes()
    {
        var constraint = Equality("notEquals", @"[""email""]");
        Assert.True(Evaluator.IsSatisfied("notEquals", constraint, Set("letter")));
    }

    [Fact]
    public void NotEquals_SameSet_Fails()
    {
        var constraint = Equality("notEquals", @"[""email"", ""phone""]");
        Assert.False(Evaluator.IsSatisfied("notEquals", constraint, Set("phone", "email")));
    }

    [Fact]
    public void NotEquals_SubsetOfExpected_IsNotEqual_Passes()
    {
        // Eine echte Teilmenge ist nicht dieselbe Menge und erfüllt deshalb notEquals.
        var constraint = Equality("notEquals", @"[""email"", ""phone""]");
        Assert.True(Evaluator.IsSatisfied("notEquals", constraint, Set("phone")));
    }



    [Fact]
    public void Equals_ScalarMatch_Passes()
    {
        var constraint = Equality("equals", @"""gruen""");
        Assert.True(Evaluator.IsSatisfied("equals", constraint, FormValue.FromString("gruen")));
    }

    [Fact]
    public void Equals_ScalarMismatch_Fails()
    {
        var constraint = Equality("equals", @"""gruen""");
        Assert.False(Evaluator.IsSatisfied("equals", constraint, FormValue.FromString("rot")));
    }

    [Fact]
    public void NotEquals_ScalarMismatch_Passes()
    {
        var constraint = Equality("notEquals", @"""gruen""");
        Assert.True(Evaluator.IsSatisfied("notEquals", constraint, FormValue.FromString("rot")));
    }
}
