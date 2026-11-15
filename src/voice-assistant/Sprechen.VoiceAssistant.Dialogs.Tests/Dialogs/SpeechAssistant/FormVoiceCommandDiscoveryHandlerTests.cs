using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormVoiceCommandDiscoveryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenFieldGroupListCommand_SpeaksRelevantFieldGroups()
    {
        var speechService = new RecordingSpeechService();
        var handler = new FormVoiceCommandDiscoveryHandler(speechService);
        var context = CreateContext(CreateDiscoveryContract());

        await handler.HandleAsync(
            context with
            {
                Request = context.Request with { CommandId = GlobalCommandIds.FieldGroupList }
            },
            CancellationToken.None);

        var call = Assert.Single(speechService.Calls);
        Assert.Contains("Es gibt 2 relevante Feldgruppen", call.Text);
        Assert.Contains("Nummer 1 ist Persönliche Daten", call.Text);
        Assert.Contains("Nummer 2 ist Adresse", call.Text);
    }

    [Fact]
    public async Task HandleAsync_WhenFieldListCommand_SpeaksFieldsOfCurrentFieldGroup()
    {
        var speechService = new RecordingSpeechService();
        var handler = new FormVoiceCommandDiscoveryHandler(speechService);
        var context = CreateContext(CreateDiscoveryContract());

        Assert.True(await context.Interpreter.TryNavigateToFieldGroupAsync("address"));

        await handler.HandleAsync(
            context with
            {
                Request = context.Request with { CommandId = GlobalCommandIds.FieldList }
            },
            CancellationToken.None);

        var call = Assert.Single(speechService.Calls);
        Assert.Contains("In der Feldgruppe Adresse gibt es 2 relevante Felder", call.Text);
        Assert.Contains("Nummer 1 ist Straße", call.Text);
        Assert.Contains("Nummer 2 ist Hausnummer", call.Text);
    }

    private static FormVoiceCommandGlobalCommandContext CreateContext(FormContract contract)
    {
        var interpreter = new FormInterpreterRuntimeOwner(contract, requestedIsoCode: "de", ImmutableDictionary<string, string>.Empty);
        var form = new FormDescriptor
        {
            FormId = FormId.From("form"),
            DisplayName = new LocalizedText { ["de"] = "Formular" },
            Description = new LocalizedText { ["de"] = "Formular" },
            LatestContractVersion = "1",
            AvailableContractVersions = ["1"]
        };

        var application = new ApplicationDescriptor
        {
            ApplicationId = "app",
            DisplayName = new LocalizedText { ["de"] = "Anwendung" },
            Description = new LocalizedText { ["de"] = "Anwendung" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            Forms = [form]
        };

        return new FormVoiceCommandGlobalCommandContext(
            Request: new FormVoiceCommandGlobalCommandRequest(
                CommandId: GlobalCommandIds.FieldGroupList,
                IsoCode: "de",
                Argument: null),
            Selection: new FormVoiceCommandSelectionContext(
                VoiceSettings: new VoiceDialogSettings(),
                Application: application,
                CompletedFormIds: ImmutableHashSet<string>.Empty.WithComparer(StringComparer.OrdinalIgnoreCase),
                Form: form,
                Contract: contract),
            Runtime: new FormVoiceCommandGlobalCommandRuntime(
                Interpreter: interpreter,
                Session: new EvaluationSession("discovery-session", EvaluationChannel.Voice, "test-app", "form-1"),
                StateTracker: new TestFormVoiceCommandDialogStateTracker()));
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
