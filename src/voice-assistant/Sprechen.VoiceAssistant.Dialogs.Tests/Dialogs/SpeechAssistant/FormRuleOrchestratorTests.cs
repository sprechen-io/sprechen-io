using System.Collections.Immutable;
using System.Text.Json;
using Moq;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormRuleOrchestratorTests
{
    [Fact]
    public void TryApplyRules_WhenScopeHasNoRules_ReturnsTrueWithoutPrompts()
    {
        var contract = new FormContract
        {
            Rules =
            [
                new RuleDefinition
                {
                    RuleId = "r1",
                    Scope = "onValueChanged",
                    When = ParseElement("{\"kind\":\"equals\",\"left\":{\"kind\":\"const\",\"value\":1},\"right\":{\"kind\":\"const\",\"value\":1}}"),
                    Then = [ParseElement("{\"kind\":\"setValue\",\"variableId\":\"flag\",\"value\":true}")]
                }
            ]
        };

        var state = new TestFormState();
        var service = CreateService(contract, state, maxIterations: 3);

        var success = service.TryApplyRules("onInit", out var promptIds);

        Assert.True(success);
        Assert.Empty(promptIds);
        Assert.Empty(state.GetVariableValuesSnapshot());
    }

    [Fact]
    public void TryApplyRules_WhenRulesConverge_AppliesChangesAndReturnsTrue()
    {
        var contract = new FormContract
        {
            Rules =
            [
                new RuleDefinition
                {
                    RuleId = "r1",
                    Scope = "onInit",
                    When = ParseElement("{\"kind\":\"equals\",\"left\":{\"kind\":\"const\",\"value\":1},\"right\":{\"kind\":\"const\",\"value\":1}}"),
                    Then = [ParseElement("{\"kind\":\"setValue\",\"variableId\":\"flag\",\"value\":true}")]
                }
            ]
        };

        var state = new TestFormState();
        var service = CreateService(contract, state, maxIterations: 3);

        var success = service.TryApplyRules("onInit", out var promptIds);

        Assert.True(success);
        Assert.Empty(promptIds);
        Assert.Equal("true", state.GetVariableValue("flag"));
    }

    [Fact]
    public void TryApplyRules_WhenRulesDoNotConverge_RestoresSnapshotAndReturnsFalse()
    {
        var contract = new FormContract
        {
            Rules =
            [
                new RuleDefinition
                {
                    RuleId = "r1",
                    Scope = "onInit",
                    When = ParseElement("{\"kind\":\"equals\",\"left\":{\"kind\":\"var\",\"variableId\":\"flag\"},\"right\":{\"kind\":\"const\",\"value\":true}}"),
                    Then = [ParseElement("{\"kind\":\"setValue\",\"variableId\":\"flag\",\"value\":false}")],
                    Else = [ParseElement("{\"kind\":\"setValue\",\"variableId\":\"flag\",\"value\":true}")]
                }
            ]
        };

        var state = new TestFormState();
        var loopGuardFeedback = new Mock<IFormRuleLoopGuardFeedback>();
        var service = CreateService(contract, state, maxIterations: 2, loopGuardFeedback);

        var success = service.TryApplyRules("onInit", out var promptIds);

        Assert.False(success);
        Assert.Empty(promptIds);
        Assert.Empty(state.GetVariableValuesSnapshot());
        loopGuardFeedback.Verify(feedback => feedback.OnRuleLoopGuardBreach(), Times.Once);
    }

    private static FormRuleOrchestrator CreateService(
        FormContract contract,
        TestFormState state,
        int maxIterations,
        Mock<IFormRuleLoopGuardFeedback>? loopGuardFeedback = null)
    {
        var ruleEngine = new FormRuleEngine(variableId =>
            ResolveRuleValue(state, variableId));
        var actionExecutor = new FormActionExecutor(
            state,
            state,
            state,
            state);

        return new FormRuleOrchestrator(
            contract,
            ruleEngine,
            actionExecutor,
            takeSnapshot: () => CreateSnapshot(state),
            restoreSnapshot: snapshot => RestoreSnapshot(state, snapshot),
            (loopGuardFeedback ?? new Mock<IFormRuleLoopGuardFeedback>()).Object,
            maxRuleIterations: maxIterations);
    }

    private static FormValue? ResolveRuleValue(
        TestFormState state,
        string variableId)
    {
        var rawValue = state.GetVariableValue(variableId);
        if (rawValue is null)
        {
            return null;
        }

        if (bool.TryParse(rawValue, out var boolValue))
        {
            return FormValue.FromBoolean(boolValue);
        }

        if (int.TryParse(rawValue, out var intValue))
        {
            return FormValue.FromInteger(intValue);
        }

        return FormValue.FromString(rawValue);
    }

    private static FormDialogStateSnapshot CreateSnapshot(TestFormState state)
    {
        return new FormDialogStateSnapshot(
            new Dictionary<string, string>(state.GetVariableValuesSnapshot(), StringComparer.Ordinal),
            new Dictionary<FieldGroupId, bool>(),
            new Dictionary<FieldId, bool>(),
            new Dictionary<FieldId, bool>(),
            new Dictionary<FieldId, bool>(),
            new Dictionary<FieldId, string>(),
            FocusedFieldId: null);
    }

    private static void RestoreSnapshot(
        TestFormState state,
        FormDialogStateSnapshot snapshot)
    {
        state.ReplaceVariableValues(snapshot.ValuesByVariableId);
    }

    private static JsonElement ParseElement(string json)
        => JsonDocument.Parse(json).RootElement;
}
