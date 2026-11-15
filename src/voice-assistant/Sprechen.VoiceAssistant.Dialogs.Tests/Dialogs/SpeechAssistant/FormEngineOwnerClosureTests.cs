using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormEngineOwnerClosureTests
{
    [Fact]
    public void InputModePromptIdResolver_LiteralMode_ResolvesLiteralPromptId()
    {
        var resolver = new FormInputModeChangedPromptIdResolver();

        var promptId = resolver.Resolve(InputMode.Literal);

        Assert.Equal(FormDialogPromptIds.InputModeChangedLiteral, promptId);
    }

    [Fact]
    public void RequiredConstraintEvaluator_StringValue_RequiresNonWhitespaceRawValue()
    {
        var evaluator = new FormRequiredConstraintEvaluator(
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["name"] = "   "
            });

        var satisfied = evaluator.IsSatisfied("name", FormValue.FromString("   "), isValueSet: true, ValueTypeKinds.String);

        Assert.False(satisfied);
    }

    [Fact]
    public void CompareVariablesConstraintEvaluator_IntegerComparison_UsesResolvedVariableValues()
    {
        var constraint = CreateCompareVariablesConstraint("left", "right", "gt");
        var evaluator = new FormCompareVariablesConstraintEvaluator(variableId => variableId switch
        {
            "left" => FormValue.FromInteger(7),
            "right" => FormValue.FromInteger(3),
            _ => null
        });

        var satisfied = evaluator.IsSatisfied(constraint);

        Assert.True(satisfied);
    }

    [Fact]
    public void ChoiceOptionsResolver_MalformedDisplayName_FallsBackToCode()
    {
        var variables = new Dictionary<VariableId, VariableDefinition>
        {
            [VariableId.From("choice")] = new()
            {
                VariableId = VariableId.From("choice"),
                ValueType = ParseJsonElement(
                    """
                    {
                      "kind": "enum",
                      "values": [
                        { "code": "yes", "displayName": { "de": 7 } }
                      ]
                    }
                    """)
            }
        };
        var resolver = new FormChoiceOptionsResolver(variables, (_, fallback) => fallback ?? string.Empty);

        var options = resolver.ResolveChoiceOptions("choice");

        var option = Assert.Single(options);
        Assert.Equal("yes", option.Code);
        Assert.Equal("yes", option.DisplayName);
    }

    [Fact]
    public void FieldGroupFocusResolver_TargetsFirstAvailableFieldOnGroupPath()
    {
        var state = CreateNavigatorState("first", "second", "third");
        state.SetFieldAvailable("first", false);
        var resolver = new FormFieldGroupFocusResolver(state);

        var moved = resolver.TryNavigateToFieldGroup("root");

        Assert.True(moved);
        Assert.Equal("second", state.CurrentFocusedFieldId);
    }

    [Fact]
    public void FocusRepairService_UnavailableCurrentFocus_RepairsForward()
    {
        var state = CreateNavigatorState("first", "second", "third");
        state.SetCurrentFocus("first");
        state.SetFieldAvailable("first", false);
        var repairService = new FormFocusRepairPolicy(state);

        repairService.EnsureFocusIsValid();

        Assert.Equal("second", state.CurrentFocusedFieldId);
    }

    [Fact]
    public void JumpTargetTextMatcher_LocalizedText_MatchesPhraseNormalizedTarget()
    {
        var localizationResolver = CreateLocalizationResolver();
        var matcher = new FormJumpTargetTextMatcher(localizationResolver);
        var text = new LocalizedText { ["de"] = "Geburtsdatum" };

        var matches = matcher.Matches(text, TextNormalizer.NormalizeForPhraseMatching("Geburtsdatum"));

        Assert.True(matches);
    }

    [Fact]
    public void ErrorViolationFilter_ReturnsOnlyErrorSeverity()
    {
        var filter = new FormErrorViolationFilter();
        var violations = ImmutableArray.Create(
            new FormConstraintViolation("field", "error", SeverityLevels.Error, ConstraintKinds.Required, "message", null, ConstraintScopes.Field, null),
            new FormConstraintViolation("field", "warning", SeverityLevels.Warning, ConstraintKinds.Required, "message", null, ConstraintScopes.Field, null));

        var errors = filter.Filter(violations);

        var error = Assert.Single(errors);
        Assert.Equal("error", error.ConstraintId);
    }

    [Fact]
    public void GlobalCommandLeadInDetector_AnchorLeadIn_ReturnsTrue()
    {
        var detector = new FormGlobalCommandLeadInDetector();

        // Der Aufrufer übergibt die akzeptierten Aktivierungsformen.
        var hasLeadIn = detector.HasCommandLeadIn("okay guide weiter", ["okay guide"]);

        Assert.True(hasLeadIn);
    }

    private static ConstraintDefinition CreateCompareVariablesConstraint(
        string leftVariableId,
        string rightVariableId,
        string operatorValue)
    {
        return new ConstraintDefinition
        {
            ConstraintId = "compare",
            Kind = ConstraintKind.CompareVariables,
            Target = new ConstraintTargetRef { Kind = TargetKinds.Variable, VariableId = VariableId.From("left") },
            Severity = ConstraintSeverity.Error,
            AdditionalProperties = new Dictionary<string, JsonElement>
            {
                [ConstraintPropertyNames.LeftVariableId] = ParseJsonElement($"\"{leftVariableId}\""),
                [ConstraintPropertyNames.RightVariableId] = ParseJsonElement($"\"{rightVariableId}\""),
                [ConstraintPropertyNames.Operator] = ParseJsonElement($"\"{operatorValue}\"")
            }
        };
    }

    private static TestNavigatorState CreateNavigatorState(params string[] fieldIds)
    {
        var orderedFieldIds = fieldIds.ToImmutableArray();
        return new TestNavigatorState(
            orderedFieldIds,
            orderedFieldIds
                .Select((fieldId, index) => (fieldId, index))
                .ToImmutableDictionary(pair => pair.fieldId, pair => pair.index, StringComparer.Ordinal),
            orderedFieldIds.ToImmutableDictionary(fieldId => fieldId, _ => "root", StringComparer.Ordinal),
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["root"] = null
            }.ToImmutableDictionary(StringComparer.Ordinal));
    }

    private static FormLocalizationResolver CreateLocalizationResolver()
    {
        return new FormLocalizationResolver(
            "de",
            defaultLanguage: "de",
            ["de", "en"],
            strictLocalization: false,
            PolitenessLevel.Formal,
            ImmutableDictionary<VariableId, VariableDefinition>.Empty,
            ImmutableDictionary<FieldGroupId, FieldGroupDefinition>.Empty);
    }

    private static JsonElement ParseJsonElement(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
