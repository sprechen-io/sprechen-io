using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormFocusedFieldSnapshotBuilderTests
{
    [Fact]
    public void Build_WhenFocusedEnumFieldHasVoiceMetadata_ProjectsLocalizedPresentationAndInteractionPolicy()
    {
        var contract = CreateContract();
        contract.Variables[0].SensitiveDataPolicy = new SensitiveDataPolicy
        {
            ReadbackPolicy = SensitiveDataPolicyDefaults.ReadbackNever
        };

        var interpreter = new FormInterpreterRuntimeOwner(contract, "de", ImmutableDictionary<string, string>.Empty);

        var snapshot = interpreter.Services.FocusedFieldSnapshotBuilder.Build();

        Assert.Equal("countryField", snapshot.FieldId);
        Assert.Equal("country", snapshot.VariableId);
        Assert.Equal("identity", snapshot.FieldGroupId);
        Assert.Equal("Identit\u00e4t", snapshot.FieldGroupTitle);
        Assert.Equal("Land", snapshot.Label);
        Assert.Equal("W\u00e4hle ein Land.", snapshot.HelpText);
        Assert.Equal("Deutschland", Assert.Single(snapshot.Examples));
        Assert.Collection(
            snapshot.Options,
            option =>
            {
                Assert.Equal("DE", option.Code);
                Assert.Equal("Deutschland", option.DisplayName);
            });
        // Buchstabieren ist für ein Auswahlfeld nicht verfügbar. Die Auflösung überspringt
        // deshalb diese Präferenz und fällt auf Spracheingabe zurück.
        Assert.Equal(InputMode.Voice, snapshot.InputMode);
        Assert.Equal(ValueTypeKinds.Enum, snapshot.ValueTypeKind);
        Assert.False(snapshot.SpeakValueInConfirmation);
    }

    [Fact]
    public void Build_WhenPolitenessIsInformal_UsesInformalFocusedFieldHelpText()
    {
        var contract = CreateContract();
        contract.Fields[0].HelpText = new LocalizedText
        {
            ["de"] = "W\u00e4hlen Sie ein Land.",
            ["de@informal"] = "W\u00e4hle ein Land."
        };

        var interpreter = new FormInterpreterRuntimeOwner(
            contract,
            "de",
            ImmutableDictionary<string, string>.Empty,
            PolitenessLevel.Informal);

        var snapshot = interpreter.Services.FocusedFieldSnapshotBuilder.Build();

        Assert.Equal("W\u00e4hle ein Land.", snapshot.HelpText);
    }

    [Fact]
    public async Task Build_WhenInputModeOverrideExists_UsesOverrideAsEffectiveInputMode()
    {
        var interpreter = new FormInterpreterRuntimeOwner(CreateContract(), "de", ImmutableDictionary<string, string>.Empty);

        Assert.True(await interpreter.TrySetFocusedFieldInputModeAsync(InputModes.Keyboard));

        var snapshot = interpreter.Services.FocusedFieldSnapshotBuilder.Build();

        Assert.Equal(InputMode.Keyboard, snapshot.InputMode);
    }

    private static FormContract CreateContract()
    {
        var enumValueType = JsonDocument.Parse("""
            {
              "kind": "enum",
              "values": [
                { "code": "DE", "displayName": { "de": "Deutschland" } }
              ]
            }
            """).RootElement;

        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "test",
            FormId = FormId.From("snapshot"),
            SupportedLanguages = ["de"],
            EntryFieldGroupId = FieldGroupId.From("identity"),
            StrictLocalization = true,
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("country"),
                    Name = "country",
                    ValueType = enumValueType
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("countryField"),
                    VariableId = VariableId.From("country"),
                    Label = new LocalizedText { ["de"] = "Land" },
                    HelpText = new LocalizedText { ["de"] = "W\u00e4hle ein Land." },
                    Examples =
                    [
                        new LocalizedText { ["de"] = " Deutschland " }
                    ],
                    PreferredInputModes = [InputModes.Spelling],
                    Editability = FieldEditability.UserEditable
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("identity"),
                    Title = new LocalizedText { ["de"] = "Identit\u00e4t" },
                    Members =
                    [
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("countryField") }
                    ]
                }
            ]
        };
    }
}
