using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormFieldCompletionPolicyTests
{
    [Fact]
    public void IsRequiredField_WhenFieldConstraintIsRequired_ReturnsTrue()
    {
        var service = CreateService(
            new FieldDefinition
            {
                FieldId = FieldId.From("nameField"),
                VariableId = VariableId.From("name"),
                FieldConstraints =
                [
                    new ConstraintDefinition
                    {
                        ConstraintId = "nameRequired",
                        Kind = ConstraintKind.Required,
                        Target = new ConstraintTargetRef { Kind = TargetKinds.Field, FieldId = FieldId.From("nameField") }
                    }
                ]
            });

        Assert.True(service.IsRequiredField("nameField"));
    }

    [Fact]
    public void IsFieldCompleted_WhenFieldIsBlocking_ReturnsFalseEvenWhenValueExists()
    {
        var service = CreateService(new FieldDefinition { FieldId = FieldId.From("nameField"), VariableId = VariableId.From("name") });

        var completed = service.IsFieldCompleted(
            "nameField",
            ["nameField"],
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["name"] = "Max"
            });

        Assert.False(completed);
    }

    private static FormFieldCompletionPolicy CreateService(FieldDefinition field)
    {
        var fieldsById = ImmutableDictionary<FieldId, FieldDefinition>.Empty
            .Add(FieldId.From(field.FieldId), field);
        var fieldToVariableId = ImmutableDictionary<FieldId, string>.Empty
            .Add(FieldId.From(field.FieldId), field.VariableId);
        var variablesById = ImmutableDictionary<VariableId, VariableDefinition>.Empty
            .Add(
                VariableId.From(field.VariableId),
                new VariableDefinition
                {
                    VariableId = field.VariableId,
                    Name = field.VariableId,
                    ValueType = JsonDocument.Parse("{\"kind\":\"string\"}").RootElement
                });

        return new FormFieldCompletionPolicy(
            fieldsById,
            fieldToVariableId,
            variablesById,
            []);
    }
}
