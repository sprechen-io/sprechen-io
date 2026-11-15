using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormDiscoveryProjectionTests
{
    [Fact]
    public async Task GetDiscoverySnapshot_UsesFieldGroupOrderAndCurrentFieldGroupFields()
    {
        var contract = CreateDiscoveryContract();
        var interpreter = new FormInterpreterRuntimeOwner(contract, "de", ImmutableDictionary<string, string>.Empty);

        Assert.True(await interpreter.TryNavigateToFieldGroupAsync("address"));

        var discovery = await interpreter.GetDiscoverySnapshotAsync();

        Assert.Equal("address", discovery.CurrentFieldGroupId);
        Assert.Equal("Adresse", discovery.CurrentFieldGroupTitle);

        Assert.Collection(
            discovery.FieldGroups,
            fieldGroup =>
            {
                Assert.Equal("personal", fieldGroup.FieldGroupId);
                Assert.Equal("Persönliche Daten", fieldGroup.Title);
                Assert.Equal(1, fieldGroup.Index);
                Assert.False(fieldGroup.IsCurrent);
            },
            fieldGroup =>
            {
                Assert.Equal("address", fieldGroup.FieldGroupId);
                Assert.Equal("Adresse", fieldGroup.Title);
                Assert.Equal(2, fieldGroup.Index);
                Assert.True(fieldGroup.IsCurrent);
            });

        Assert.Collection(
            discovery.CurrentFieldGroupFields,
            field =>
            {
                Assert.Equal("streetField", field.FieldId);
                Assert.Equal("Straße", field.Label);
                Assert.Equal(1, field.Index);
                Assert.True(field.IsCurrent);
            },
            field =>
            {
                Assert.Equal("houseNumberField", field.FieldId);
                Assert.Equal("Hausnummer", field.Label);
                Assert.Equal(2, field.Index);
                Assert.False(field.IsCurrent);
            });
    }

    private static FormContract CreateDiscoveryContract()
    {
        var stringValueType = JsonDocument.Parse("""{"kind":"string"}""").RootElement;

        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "app",
            FormId = FormId.From("form"),
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            StrictLocalization = true,
            EntryFieldGroupId = FieldGroupId.From("personal"),
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("firstName"),
                    Name = "firstName",
                    ValueType = stringValueType
                },
                new VariableDefinition
                {
                    VariableId = VariableId.From("street"),
                    Name = "street",
                    ValueType = stringValueType
                },
                new VariableDefinition
                {
                    VariableId = VariableId.From("houseNumber"),
                    Name = "houseNumber",
                    ValueType = stringValueType
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("firstNameField"),
                    VariableId = VariableId.From("firstName"),
                    Label = new LocalizedText { ["de"] = "Vorname" },
                    Editability = FieldEditability.UserEditable
                },
                new FieldDefinition
                {
                    FieldId = FieldId.From("streetField"),
                    VariableId = VariableId.From("street"),
                    Label = new LocalizedText { ["de"] = "Straße" },
                    Editability = FieldEditability.UserEditable
                },
                new FieldDefinition
                {
                    FieldId = FieldId.From("houseNumberField"),
                    VariableId = VariableId.From("houseNumber"),
                    Label = new LocalizedText { ["de"] = "Hausnummer" },
                    Editability = FieldEditability.UserEditable
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("personal"),
                    Title = new LocalizedText { ["de"] = "Persönliche Daten" },
                    Members =
                    [
                        new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From("firstNameField") },
                        new FieldGroupMember { Kind = MemberKinds.FieldGroup, FieldGroupId = FieldGroupId.From("address") }
                    ]
                },
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("address"),
                    Title = new LocalizedText { ["de"] = "Adresse" },
                    Members =
                    [
                        new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From("streetField") },
                        new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From("houseNumberField") }
                    ]
                }
            ]
        };
    }
}
