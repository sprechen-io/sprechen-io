using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;
using Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft oneOf für Mengenwerte: Jeder Eintrag beschreibt eine zulässige vollständige Menge,
/// deren Reihenfolge unerheblich ist. Skalare Einträge gelten als einelementige Mengen.
/// Weitere Fälle prüfen skalare Werte und eine fehlende values-Eigenschaft.
/// </summary>
public sealed class FormOneOfConstraintEvaluatorTests
{
    private static readonly FormOneOfConstraintEvaluator Evaluator = new();

    private static ConstraintDefinition OneOf(params JsonElement[] values)
    {
        using var document = JsonDocument.Parse($@"{{
            ""constraintId"": ""c1"",
            ""target"": {{ ""kind"": ""field"", ""fieldId"": ""channels"" }},
            ""severity"": ""error"",
            ""code"": ""test.oneof"",
            ""kind"": ""oneOf"",
            ""values"": [{string.Join(",", values.Select(v => v.GetRawText()))}]
        }}");
        return JsonSerializer.Deserialize<ConstraintDefinition>(document.RootElement.GetRawText())
            ?? throw new InvalidOperationException("constraint deserialization failed");
    }

    private static JsonElement El(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }



    [Fact]
    public void SetValue_ExactSingleElementEntry_Passes()
    {
        var constraint = OneOf(El(@"""email"""));
        var value = FormValue.FromSet(["email"]);
        Assert.True(Evaluator.IsSatisfied(constraint, value));
    }

    [Fact]
    public void SetValue_SupersetOfSingleElementEntry_Fails()
    {
        var constraint = OneOf(El(@"""email"""));
        var value = FormValue.FromSet(["email", "phone"]);
        Assert.False(Evaluator.IsSatisfied(constraint, value));
    }

    [Fact]
    public void SetValue_SubsetOfMultiElementEntry_Fails()
    {
        var constraint = OneOf(El(@"[""email"", ""phone""]"));
        var value = FormValue.FromSet(["phone"]);
        Assert.False(Evaluator.IsSatisfied(constraint, value));
    }

    [Fact]
    public void SetValue_SameSetDifferentOrder_Passes()
    {
        var constraint = OneOf(El(@"[""email"", ""phone""]"));
        var value = FormValue.FromSet(["phone", "email"]);
        Assert.True(Evaluator.IsSatisfied(constraint, value));
    }

    [Fact]
    public void SetValue_DuplicatesNormalizedAway_Passes()
    {
        var constraint = OneOf(El(@"[""email"", ""phone""]"));
        var value = FormValue.FromSet(["phone", "email", "email", "phone"]);
        Assert.True(Evaluator.IsSatisfied(constraint, value));
    }

    [Fact]
    public void SetValue_ForeignElement_Fails()
    {
        var constraint = OneOf(El(@"[""email"", ""phone""]"));
        var value = FormValue.FromSet(["letter"]);
        Assert.False(Evaluator.IsSatisfied(constraint, value));
    }

    [Fact]
    public void SetValue_OneOfSeveralEntries_PassesOnSecond()
    {
        var constraint = OneOf(El(@"""email"""), El(@"[""phone"", ""letter""]"));
        var value = FormValue.FromSet(["letter", "phone"]);
        Assert.True(Evaluator.IsSatisfied(constraint, value));
    }

    [Fact]
    public void SetValue_EmptySetValue_Fails()
    {
        var constraint = OneOf(El(@"""email"""));
        var value = FormValue.FromSet([]);
        Assert.False(Evaluator.IsSatisfied(constraint, value));
    }



    [Fact]
    public void ScalarValue_MemberOfValues_Passes()
    {
        var constraint = OneOf(El(@"""gruen"""));
        Assert.True(Evaluator.IsSatisfied(constraint, FormValue.FromString("gruen")));
    }

    [Fact]
    public void ScalarValue_NotMember_Fails()
    {
        var constraint = OneOf(El(@"""gruen"""));
        Assert.False(Evaluator.IsSatisfied(constraint, FormValue.FromString("rot")));
    }

    [Fact]
    public void ScalarValue_NumericEntry_Passes()
    {
        var constraint = OneOf(El(@"3"));
        Assert.True(Evaluator.IsSatisfied(constraint, FormValue.FromInteger(3)));
    }

    [Fact]
    public void MissingValuesProperty_Passes()
    {
        using var document = JsonDocument.Parse(@"{ ""constraintId"": ""c1"", ""kind"": ""oneOf"" }");
        var constraint = JsonSerializer.Deserialize<ConstraintDefinition>(document.RootElement.GetRawText())!;
        Assert.True(Evaluator.IsSatisfied(constraint, FormValue.FromSet(["email"])));
    }
}
