using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormVoiceCommandAmbiguityLabelResolverTests
{
    private readonly FormVoiceCommandAmbiguityLabelResolver _resolver = new();

    [Fact]
    public void Resolve_WhenCommandLabelExists_ReturnsLocalizedLabel()
    {
        var context = CreateContext();
        var interpretation = CreateInterpretation(GlobalCommandIds.Next);

        var label = _resolver.Resolve(context, interpretation);

        Assert.False(string.IsNullOrWhiteSpace(label));
        Assert.NotEqual(GlobalCommandIds.Next, label);
    }

    [Fact]
    public void Resolve_WhenCommandLabelIsMissing_ReturnsCommandId()
    {
        var context = CreateContext();
        var interpretation = CreateInterpretation("custom-command");

        var label = _resolver.Resolve(context, interpretation);

        Assert.Equal("custom-command", label);
    }

    private static FormVoiceCommandTranscriptInterpretation CreateInterpretation(string parsedCommandId)
        => new(
            new FormVoiceCommandTranscriptSource(
                parsedCommandId,
                parsedCommandId,
                false),
            new FormVoiceCommandParsedCommand(
                true,
                parsedCommandId,
                null,
                CommandSignalClass.Ambiguous),
            new FormVoiceCommandInterpretationMode(
                true,
                false,
                false,
                false,
                false));

    private static FormVoiceCommandContext CreateContext()
    {
        var contract = new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "app",
            FormId = FormId.From("current"),
            DisplayName = new LocalizedText { ["de"] = "Formular" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            StrictLocalization = true,
            EntryFieldGroupId = FieldGroupId.From("main"),
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("name"),
                    Name = "name",
                    ValueType = JsonDocument.Parse("""{"kind":"string"}""").RootElement
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("name"),
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
                        new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From("name") }
                    ]
                }
            ]
        };

        return new FormVoiceCommandContext(
            SessionIdentity: new FormVoiceCommandSessionIdentity(
                IsoCode: "de",
                ApplicationId: "app",
                FormId: "current",
                WakeWordActivationForms: []),
            Selection: new FormVoiceCommandSelectionContext(
                VoiceSettings: new VoiceDialogSettings(),
                Application: new ApplicationDescriptor
                {
                    ApplicationId = "app",
                    DisplayName = new LocalizedText { ["de"] = "App" },
                    SupportedLanguages = ["de"],
                    DefaultLanguage = "de",
                    Forms =
                    [
                        new FormDescriptor
                        {
                            FormId = FormId.From("current"),
                            DisplayName = new LocalizedText { ["de"] = "Formular" },
                            LatestContractVersion = "1",
                            AvailableContractVersions = ["1"]
                        }
                    ]
                },
                CompletedFormIds: ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase),
                Form: new FormDescriptor
                {
                    FormId = FormId.From("current"),
                    DisplayName = new LocalizedText { ["de"] = "Formular" },
                    LatestContractVersion = "1",
                    AvailableContractVersions = ["1"]
                },
                Contract: contract),
            Values: new FormVoiceCommandValueSnapshot(
                ValuesByVariableId: ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
                SharedAdoptions: []));
    }
}
