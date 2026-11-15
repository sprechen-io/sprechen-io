using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;
using Xunit;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormAllOrNoneConstraintEvaluatorTests
{
    [Fact]
    public void IsSatisfied_WhenOneVariableIsSetAndAnotherIsNot_ReturnsFalse()
    {
        var evaluator = CreateEvaluator(variableId =>
            variableId == "first" ? FormValue.FromString("Wert") : null);
        var constraint = CreateConstraintWithVariableIds("first", "second");

        var result = evaluator.IsSatisfied(constraint);

        Assert.False(result);
    }

    [Fact]
    public void IsSatisfied_WhenEveryVariableIsSet_ReturnsTrue()
    {
        var evaluator = CreateEvaluator(_ => FormValue.FromString("Wert"));
        var constraint = CreateConstraintWithVariableIds("first", "second");

        var result = evaluator.IsSatisfied(constraint);

        Assert.True(result);
    }

    [Fact]
    public void IsSatisfied_WhenNoVariableIsSet_ReturnsTrue()
    {
        var evaluator = CreateEvaluator(_ => null);
        var constraint = CreateConstraintWithVariableIds("first", "second");

        var result = evaluator.IsSatisfied(constraint);

        Assert.True(result);
    }

    [Fact]
    public void IsSatisfied_WhenConstraintNamesNoVariableIds_ReturnsTrue()
    {
        var evaluator = CreateEvaluator(variableId =>
            variableId == "first" ? FormValue.FromString("Wert") : null);
        var constraint = CreateConstraintWithoutVariableIds();

        var result = evaluator.IsSatisfied(constraint);

        Assert.True(result);
    }

    [Fact]
    public void IsSatisfied_WhenVariableIdsIsNotAnArray_ReturnsFalse()
    {
        var evaluator = CreateEvaluator(_ => null);
        var constraint = CreateConstraintWithAdditionalProperties(
            new Dictionary<string, JsonElement>(StringComparer.Ordinal)
            {
                [ConstraintPropertyNames.VariableIds] = JsonSerializer.SerializeToElement("first")
            });

        var result = evaluator.IsSatisfied(constraint);

        Assert.False(result);
    }

    private static FormAllOrNoneConstraintEvaluator CreateEvaluator(Func<string, FormValue?> getVariableValue)
        => new(new FormVariableValuePresenceResolver(getVariableValue));

    private static ConstraintDefinition CreateConstraintWithVariableIds(params string[] variableIds)
        => CreateConstraintWithAdditionalProperties(
            new Dictionary<string, JsonElement>(StringComparer.Ordinal)
            {
                [ConstraintPropertyNames.VariableIds] = JsonSerializer.SerializeToElement(variableIds)
            });

    private static ConstraintDefinition CreateConstraintWithoutVariableIds()
        => CreateConstraintWithAdditionalProperties(new Dictionary<string, JsonElement>(StringComparer.Ordinal));

    private static ConstraintDefinition CreateConstraintWithAdditionalProperties(
        Dictionary<string, JsonElement> additionalProperties)
    {
        return new ConstraintDefinition
        {
            ConstraintId = "constraint.allOrNone",
            Kind = ConstraintKind.AllOrNone,
            Target = new ConstraintTargetRef { Kind = TargetKinds.Variable, VariableId = VariableId.From("value") },
            Severity = ConstraintSeverity.Error,
            Message = new LocalizedText { ["de"] = "Fehler" },
            AdditionalProperties = additionalProperties
        };
    }
}
