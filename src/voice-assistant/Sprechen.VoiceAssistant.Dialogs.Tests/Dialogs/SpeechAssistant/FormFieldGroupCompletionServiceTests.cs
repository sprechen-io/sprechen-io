using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormFieldGroupCompletionServiceTests
{
    [Fact]
    public void CountCompletedFieldGroups_WhenRequiredFieldIsBlocked_ReturnsZero()
    {
        var service = CreateService();

        var completed = service.CountCompletedFieldGroups(
            ["main"],
            ["nameField"],
            ["nameField"],
            ImmutableHashSet<string>.Empty.WithComparer(StringComparer.Ordinal),
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["name"] = "Max"
            });

        Assert.Equal(0, completed);
    }

    [Fact]
    public void BuildScopedBlockingFieldGroupIds_WhenConstraintScopeIsFieldGroup_BlocksOnlyThatFieldGroup()
    {
        var service = CreateService(
            new ConstraintDefinition
            {
                ConstraintId = "mainGroupInvalid",
                Scope = ConstraintScope.FieldGroup,
                Target = new ConstraintTargetRef { Kind = TargetKinds.Field, FieldId = FieldId.From("nameField") }
            });

        var blockingGroups = service.BuildScopedBlockingFieldGroupIds(
            [CreateViolation("mainGroupInvalid", "nameField", "main")],
            ["main", "other"]);

        Assert.Equal(["main"], blockingGroups.Order(StringComparer.Ordinal));
    }

    private static FormFieldGroupCompletionService CreateService(params ConstraintDefinition[] constraints)
    {
        var fieldToFieldGroupId = ImmutableDictionary<FieldId, string>.Empty
            .Add(FieldId.From("nameField"), "main");
        var fieldCompletionService = new FormFieldCompletionPolicy(
            ImmutableDictionary<FieldId, FieldDefinition>.Empty.Add(
                FieldId.From("nameField"),
                new FieldDefinition { FieldId = FieldId.From("nameField"), VariableId = VariableId.From("name") }),
            ImmutableDictionary<FieldId, string>.Empty.Add(FieldId.From("nameField"), "name"),
            ImmutableDictionary<VariableId, VariableDefinition>.Empty.Add(
                VariableId.From("name"),
                new VariableDefinition
                {
                    VariableId = VariableId.From("name"),
                    Name = "name",
                    ValueType = JsonDocument.Parse("{\"kind\":\"string\"}").RootElement
                }),
            constraints.ToImmutableArray());

        return new FormFieldGroupCompletionService(
            fieldToFieldGroupId,
            constraints.ToImmutableArray(),
            fieldCompletionService);
    }

    private static FormConstraintViolation CreateViolation(string constraintId, string fieldId, string? fieldGroupId)
        => new(
            fieldId,
            constraintId,
            SeverityLevels.Error,
            constraintId,
            constraintId,
            Remediation: null,
            ConstraintScopes.FieldGroup,
            fieldGroupId);
}
