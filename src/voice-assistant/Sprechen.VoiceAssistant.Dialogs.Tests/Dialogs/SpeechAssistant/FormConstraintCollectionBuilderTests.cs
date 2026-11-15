using System.Text.Json;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormConstraintCollectionBuilderTests
{
    [Fact]
    public void Build_AggregatesTopLevelFieldAndInvariantConstraints()
    {
        var contract = new FormContract
        {
            Constraints =
            [
                new ConstraintDefinition
                {
                    ConstraintId = "c.global",
                    Kind = ConstraintKind.Required,
                    Target = new ConstraintTargetRef { Kind = TargetKinds.Form }
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("f1"),
                    VariableId = VariableId.From("v1"),
                    FieldConstraints =
                    [
                        new ConstraintDefinition
                        {
                            ConstraintId = "c.field",
                            Kind = ConstraintKind.Pattern,
                            Target = new ConstraintTargetRef { Kind = TargetKinds.Field, FieldId = FieldId.From("f1") }
                        }
                    ]
                }
            ],
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("v1"),
                    Name = "v1",
                    ValueType = JsonDocument.Parse("{\"kind\":\"string\"}").RootElement,
                    InvariantConstraints =
                    [
                        JsonDocument.Parse("{\"constraintId\":\"c.invariant\",\"kind\":\"required\",\"target\":{\"kind\":\"variable\",\"variableId\":\"v1\"}}").RootElement
                    ]
                }
            ]
        };

        var constraints = FormConstraintCollectionBuilder.Build(contract);

        Assert.Equal(3, constraints.Length);
        Assert.Contains(constraints, item => item.ConstraintId == "c.global");
        Assert.Contains(constraints, item => item.ConstraintId == "c.field");
        Assert.Contains(constraints, item => item.ConstraintId == "c.invariant");
    }
}

