using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormDialogRuntimeFactoryTests
{
    [Fact]
    public async Task Create_ReturnsRuntimeWithConfiguredContractAndParser()
    {
        var contract = CreateContract();
        var context = CreateContext(contract);
        var factory = new FormDialogRuntimeFactory(new FormTranscriptParser(new YesNoTranscriptClassifier(), new ModeledNegationParticleResolver()));

        var runtime = factory.Create(context, "session-1", NullFormRuleLoopGuardFeedback.Instance);

        Assert.IsType<FormInterpreterRuntimeOwner>(runtime);
        Assert.Equal("nameField", (await runtime.GetFocusedFieldAsync()).FieldId);

        var inputResult = await runtime.TryApplyTranscriptAsync("Max");

        Assert.True(inputResult.ValueCaptured);
        Assert.Equal("Max", (await runtime.GetValuesSnapshotAsync())["name"]);
    }

    private static FormContract CreateContract()
    {
        var stringValueType = JsonDocument.Parse("{\"kind\":\"string\"}").RootElement;

        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "app",
            FormId = FormId.From("form"),
            SupportedLanguages = ["de"],
            EntryFieldGroupId = FieldGroupId.From("main"),
            StrictLocalization = true,
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("name"),
                    Name = "name",
                    ValueType = stringValueType
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("nameField"),
                    VariableId = VariableId.From("name"),
                    Label = new LocalizedText { ["de"] = "Name" },
                    Editability = FieldEditability.UserEditable
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("main"),
                    Title = new LocalizedText { ["de"] = "Main" },
                    Members =
                    [
                        new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From("nameField") }
                    ]
                }
            ]
        };
    }

    private static FormVoiceCommandContext CreateContext(FormContract contract)
    {
        var form = new FormDescriptor
        {
            FormId = FormId.From("form"),
            DisplayName = new LocalizedText { ["de"] = "Form" },
            Description = new LocalizedText { ["de"] = "Form" },
            LatestContractVersion = "1",
            AvailableContractVersions = ["1"]
        };

        var application = new ApplicationDescriptor
        {
            ApplicationId = "app",
            DisplayName = new LocalizedText { ["de"] = "App" },
            Description = new LocalizedText { ["de"] = "App" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            Forms = [form]
        };

        return new FormVoiceCommandContext(
            SessionIdentity: new FormVoiceCommandSessionIdentity(
                IsoCode: "de",
                ApplicationId: "app",
                FormId: "form",
                WakeWordActivationForms: []),
            Selection: new FormVoiceCommandSelectionContext(
                VoiceSettings: new VoiceDialogSettings(),
                Application: application,
                CompletedFormIds: ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase),
                Form: form,
                Contract: contract),
            Values: new FormVoiceCommandValueSnapshot(
                ValuesByVariableId: ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
                SharedAdoptions: []));
    }
}
