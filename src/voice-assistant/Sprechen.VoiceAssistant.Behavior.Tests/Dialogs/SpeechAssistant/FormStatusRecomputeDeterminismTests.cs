using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;
using Sprechen.VoiceAssistant.FormEngine.Dialogs.SpeechAssistant;
using Sprechen.VoiceAssistant.InputParsing.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Behavior.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft reproduzierbare Fortschrittsprojektionen beim Ausblenden eines Feldes durch eine Regel.
/// Dieselbe Eingabe auf gleichen Ausgangszuständen muss gleiche Zähler liefern; erneute Projektion und erneute Regelanwendung sollen sie nicht verändern.
/// </summary>
public sealed class FormStatusRecomputeDeterminismTests
{
    private const string FieldGroupOneId = "g1";
    private const string FieldGroupTwoId = "g2";
    private const string FirstFieldId = "f1";
    private const string SecondFieldId = "f2";
    private const string ThirdFieldId = "f3";
    private const string FirstVariableId = "v1";
    private const string SecondVariableId = "v2";
    private const string ThirdVariableId = "v3";
    private const string RelevanceRuleScope = "review";
    private const int MaxRuleIterations = 8;

    [Fact]
    public void Recompute_AcrossTransition_ReproducesSameCountsAndIsIdempotent()
    {
        var firstRunState = CreateInitialState();
        var secondRunState = CreateInitialState();

        var (firstRunBefore, firstRunAfter) = RunRelevanceTransition(firstRunState);
        var (secondRunBefore, secondRunAfter) = RunRelevanceTransition(secondRunState);
        var recomputedAfter = BuildProjection(firstRunState);
        var twiceRecomputedAfter = BuildProjection(firstRunState);

        // Die Regel muss die Anzahl relevanter Felder und Gruppen tatsächlich verändern.
        Assert.Equal(new FormStatusProgressDimension(0, 3), firstRunBefore.Progress.AllFields);
        Assert.Equal(new FormStatusProgressDimension(1, 2), firstRunAfter.Progress.AllFields);
        Assert.Equal(new FormStatusProgressDimension(0, 3), firstRunBefore.Progress.RequiredFieldsOnly);
        Assert.Equal(new FormStatusProgressDimension(1, 2), firstRunAfter.Progress.RequiredFieldsOnly);
        Assert.Equal(2, firstRunBefore.Progress.FieldGroupBased.Total);
        Assert.Equal(1, firstRunAfter.Progress.FieldGroupBased.Total);
        Assert.NotEqual(firstRunBefore, firstRunAfter);

        // Gleiche Ausgangszustände und Eingaben müssen gleiche Projektionen liefern.
        Assert.Equal(firstRunBefore, secondRunBefore);
        Assert.Equal(firstRunAfter, secondRunAfter);

        // Wiederholte Projektion desselben Endzustands darf die Zähler nicht verändern.
        Assert.Equal(firstRunAfter, recomputedAfter);
        Assert.Equal(firstRunAfter, twiceRecomputedAfter);
    }

    [Fact]
    public void Recompute_ReapplyingTransitionOnSameView_ConvergesWithoutChangingCounts()
    {
        var state = CreateInitialState();
        var (_, afterTransition) = RunRelevanceTransition(state);
        var orchestrator = CreateOrchestrator(state);

        // Erneute Regelanwendung auf dem Endzustand muss konvergieren.
        var converged = orchestrator.TryApplyRules(RelevanceRuleScope, out _);
        var reprojected = BuildProjection(state);

        Assert.True(converged);
        Assert.Equal(afterTransition, reprojected);
    }

    private static (FormStatusSnapshot Before, FormStatusSnapshot After) RunRelevanceTransition(
        RecordingFormDialogState state)
    {
        var before = BuildProjection(state);

        // Zuerst die Eingabe setzen; die anschließende Regel blendet f3 aus.
        state.SetVariableValue(VariableId.From(FirstVariableId), "yes");
        var orchestrator = CreateOrchestrator(state);
        var converged = orchestrator.TryApplyRules(RelevanceRuleScope, out _);
        Assert.True(converged);

        var after = BuildProjection(state);
        return (before, after);
    }

    private static FormStatusSnapshot BuildProjection(RecordingFormDialogState state)
    {
        var builder = new FormStatusSnapshotProjectionBuilder(
            CreateScopeResolver(),
            CreateFieldCompletionPolicy(),
            CreateFieldGroupCompletionService(),
            new FormErrorViolationFilter());

        return builder.BuildStatusSnapshot(
            CreateFocusedField(),
            state.ValuesByVariableId,
            [],
            fieldId => state.IsFieldVisible(FieldId.From(fieldId)));
    }

    private static FormRuleOrchestrator CreateOrchestrator(RecordingFormDialogState state)
    {
        var ruleEngine = new FormRuleEngine(variableId =>
        {
            var raw = state.GetVariableValue(VariableId.From(variableId));
            return raw is null ? null : FormValue.FromString(raw);
        });

        var actionExecutor = new FormActionExecutor(state, state, state, state);

        return new FormRuleOrchestrator(
            CreateContractWithRelevanceRule(),
            ruleEngine,
            actionExecutor,
            () => FormDialogStateSnapshotStore.Take(
                state.ValuesByVariableId,
                state.FieldGroupVisibleById,
                state.FieldVisibleById,
                state.FieldEnabledById,
                state.FieldReadOnlyById,
                state.FieldInputModeOverrides,
                state.FocusedFieldId),
            snapshot => state.RestoreSnapshot(snapshot),
            NullFormRuleLoopGuardFeedback.Instance,
            MaxRuleIterations);
    }

    private static RecordingFormDialogState CreateInitialState()
    {
        var state = new RecordingFormDialogState();
        state.FieldVisibleById[FieldId.From(FirstFieldId)] = true;
        state.FieldVisibleById[FieldId.From(SecondFieldId)] = true;
        state.FieldVisibleById[FieldId.From(ThirdFieldId)] = true;
        state.FieldGroupVisibleById[FieldGroupId.From(FieldGroupOneId)] = true;
        state.FieldGroupVisibleById[FieldGroupId.From(FieldGroupTwoId)] = true;
        state.FocusedFieldId = FirstFieldId;
        return state;
    }

    private static FormContract CreateContractWithRelevanceRule()
    {
        return new FormContract
        {
            Rules =
            [
                new RuleDefinition
                {
                    RuleId = "r-hide-f3",
                    Priority = 0,
                    Scope = RelevanceRuleScope,
                    When = ParseJson($$"""{"kind":"isSet","variableId":"{{FirstVariableId}}"}"""),
                    Then =
                    [
                        ParseJson($$"""
                        {"kind":"setVisible","target":{"kind":"field","fieldId":"{{ThirdFieldId}}"},"value":false}
                        """)
                    ]
                }
            ]
        };
    }

    private static FormDialogProjectionScopeResolver CreateScopeResolver()
        => new(
            [FieldGroupOneId, FieldGroupTwoId],
            [FirstFieldId, SecondFieldId, ThirdFieldId],
            CreateFieldToFieldGroupId());

    private static FormFieldCompletionPolicy CreateFieldCompletionPolicy()
        => new(
            CreateFieldsById(),
            CreateFieldToVariableId(),
            CreateVariablesById(),
            []);

    private static FormFieldGroupCompletionService CreateFieldGroupCompletionService()
        => new(
            CreateFieldToFieldGroupId(),
            [],
            CreateFieldCompletionPolicy());

    private static ImmutableDictionary<FieldId, string> CreateFieldToFieldGroupId()
        => ImmutableDictionary.CreateRange(
        [
            KeyValuePair.Create(FieldId.From(FirstFieldId), FieldGroupOneId),
            KeyValuePair.Create(FieldId.From(SecondFieldId), FieldGroupOneId),
            KeyValuePair.Create(FieldId.From(ThirdFieldId), FieldGroupTwoId)
        ]);

    private static ImmutableDictionary<FieldId, string> CreateFieldToVariableId()
        => ImmutableDictionary.CreateRange(
        [
            KeyValuePair.Create(FieldId.From(FirstFieldId), FirstVariableId),
            KeyValuePair.Create(FieldId.From(SecondFieldId), SecondVariableId),
            KeyValuePair.Create(FieldId.From(ThirdFieldId), ThirdVariableId)
        ]);

    private static ImmutableDictionary<FieldId, FieldDefinition> CreateFieldsById()
        => ImmutableDictionary.CreateRange(
        [
            KeyValuePair.Create(FieldId.From(FirstFieldId), CreateRequiredField(FirstFieldId, FirstVariableId)),
            KeyValuePair.Create(FieldId.From(SecondFieldId), CreateRequiredField(SecondFieldId, SecondVariableId)),
            KeyValuePair.Create(FieldId.From(ThirdFieldId), CreateRequiredField(ThirdFieldId, ThirdVariableId))
        ]);

    private static ImmutableDictionary<VariableId, VariableDefinition> CreateVariablesById()
        => ImmutableDictionary.CreateRange(
        [
            KeyValuePair.Create(VariableId.From(FirstVariableId), new VariableDefinition { VariableId = VariableId.From(FirstVariableId) }),
            KeyValuePair.Create(VariableId.From(SecondVariableId), new VariableDefinition { VariableId = VariableId.From(SecondVariableId) }),
            KeyValuePair.Create(VariableId.From(ThirdVariableId), new VariableDefinition { VariableId = VariableId.From(ThirdVariableId) })
        ]);

    private static FieldDefinition CreateRequiredField(string fieldId, string variableId)
        => new()
        {
            FieldId = FieldId.From(fieldId),
            VariableId = VariableId.From(variableId),
            FieldConstraints = [new ConstraintDefinition { Kind = ConstraintKind.Required }]
        };

    private static FormFieldSnapshot CreateFocusedField()
        => new(
            new FormFieldIdentitySnapshot(FirstFieldId, FirstVariableId, FieldGroupOneId),
            new FormFieldPresentationSnapshot("Group One", "First", null, [], []),
            new FormFieldInteractionSnapshot(false, InputMode.Voice, "editable", ValueTypeKinds.String, false));

    private static JsonElement ParseJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
