using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormContractTopologyBuilderTests
{
    [Fact]
    public void Build_WithNestedFieldGroupsAndDuplicateMembers_ProducesStableTopology()
    {
        var contract = new FormContract
        {
            EntryFieldGroupId = FieldGroupId.From("root"),
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("root"),
                    Members =
                    [
                        new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From("f1") },
                        new FieldGroupMember { Kind = MemberKinds.FieldGroup, FieldGroupId = FieldGroupId.From("child1") },
                        new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From("f1") }
                    ]
                },
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("child1"),
                    Members =
                    [
                        new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From("f2") },
                        new FieldGroupMember { Kind = MemberKinds.FieldGroup, FieldGroupId = FieldGroupId.From("child2") }
                    ]
                },
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("child2"),
                    Members =
                    [
                        new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From("f2") },
                        new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From("f3") }
                    ]
                }
            ],
            Fields =
            [
                new FieldDefinition { FieldId = FieldId.From("f1"), VariableId = VariableId.From("v1") },
                new FieldDefinition { FieldId = FieldId.From("f2"), VariableId = VariableId.From("v2") },
                new FieldDefinition { FieldId = FieldId.From("f3"), VariableId = VariableId.From("v3") }
            ]
        };

        var fieldsById = contract.Fields
            .ToImmutableDictionary(field => FieldId.From(field.FieldId));
        var fieldGroupsById = contract.FieldGroups
            .ToImmutableDictionary(fieldGroup => FieldGroupId.From(fieldGroup.FieldGroupId));

        var topology = FormContractTopologyBuilder.Build(contract, fieldGroupsById, fieldsById);

        Assert.Collection(
            topology.OrderedFieldGroupIds,
            fieldGroupId => Assert.Equal("root", fieldGroupId),
            fieldGroupId => Assert.Equal("child1", fieldGroupId),
            fieldGroupId => Assert.Equal("child2", fieldGroupId));
        Assert.Collection(
            topology.OrderedFieldIds,
            fieldId => Assert.Equal("f1", fieldId),
            fieldId => Assert.Equal("f2", fieldId),
            fieldId => Assert.Equal("f3", fieldId));
        Assert.Equal("root", topology.FieldToFieldGroupId[FieldId.From("f1")]);
        Assert.Equal("child1", topology.FieldToFieldGroupId[FieldId.From("f2")]);
        Assert.Equal("child2", topology.FieldToFieldGroupId[FieldId.From("f3")]);
        Assert.Equal("v1", topology.FieldToVariableId[FieldId.From("f1")]);
        Assert.Equal("v2", topology.FieldToVariableId[FieldId.From("f2")]);
        Assert.Equal("v3", topology.FieldToVariableId[FieldId.From("f3")]);
        Assert.Null(topology.ParentFieldGroupById[FieldGroupId.From("root")]);
        Assert.Equal("root", topology.ParentFieldGroupById[FieldGroupId.From("child1")]);
        Assert.Equal("child1", topology.ParentFieldGroupById[FieldGroupId.From("child2")]);
    }

    [Fact]
    public void Build_WithUnknownEntrySection_FailsClosed()
    {
        var contract = new FormContract
        {
            EntryFieldGroupId = FieldGroupId.From("missing"),
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("root"),
                    Members = []
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("f1"),
                    VariableId = VariableId.From("v1"),
                    Editability = FieldEditability.UserEditable
                }
            ],
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("v1"),
                    Name = "v1",
                    ValueType = JsonDocument.Parse("{\"kind\":\"string\"}").RootElement
                }
            ]
        };

        var fieldsById = contract.Fields
            .ToImmutableDictionary(field => FieldId.From(field.FieldId));
        var fieldGroupsById = contract.FieldGroups
            .ToImmutableDictionary(fieldGroup => FieldGroupId.From(fieldGroup.FieldGroupId));

        Assert.Throws<InvalidOperationException>(() =>
            FormContractTopologyBuilder.Build(contract, fieldGroupsById, fieldsById));
    }
}



