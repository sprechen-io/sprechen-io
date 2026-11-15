using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormVoiceCommandAmbiguousSignalDetectorTests
{
    private readonly FormVoiceCommandAmbiguousSignalDetector _detector = new();

    [Fact]
    public void TryDetect_WhenTextFieldCommandHasNoExplicitSignal_ReturnsFalse()
    {
        var context = CreateContext();
        var interpretation = CreateInterpretation(
            parsedCommandId: GlobalCommandIds.Next,
            trimmedTranscript: "weiter",
            inputTranscript: "weiter");

        var detected = _detector.TryDetect(context, interpretation, out _);

        Assert.False(detected);
    }

    [Fact]
    public void TryDetect_WhenExplicitCommandPrefix_ReturnsFalse()
    {
        var context = CreateContext();
        var interpretation = CreateInterpretation(
            parsedCommandId: GlobalCommandIds.Next,
            trimmedTranscript: "kommando weiter",
            inputTranscript: "kommando weiter",
            hasExplicitCommandPrefix: true);

        var detected = _detector.TryDetect(context, interpretation, out _);

        Assert.False(detected);
    }

    [Fact]
    public void TryDetect_WhenRepeatIsEmbeddedInput_ReturnsFalse()
    {
        var context = CreateContext();
        var interpretation = CreateInterpretation(
            parsedCommandId: GlobalCommandIds.Repeat,
            trimmedTranscript: "meine adresse ist wiederholen strasse 1",
            inputTranscript: "meine adresse ist wiederholen strasse 1");

        var detected = _detector.TryDetect(context, interpretation, out _);

        Assert.False(detected);
    }

    private static FormVoiceCommandTranscriptInterpretation CreateInterpretation(
        string parsedCommandId,
        string trimmedTranscript,
        string inputTranscript,
        bool hasExplicitCommandPrefix = false)
        => new(
            new FormVoiceCommandTranscriptSource(
                trimmedTranscript,
                inputTranscript,
                hasExplicitCommandPrefix),
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
