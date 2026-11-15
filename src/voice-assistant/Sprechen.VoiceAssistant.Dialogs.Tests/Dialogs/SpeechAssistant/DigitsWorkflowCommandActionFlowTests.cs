using System.Collections.Immutable;
using System.Text.Json;
using Moq;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class DigitsWorkflowCommandActionFlowTests
{
    [Theory]
    [InlineData("de")]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("it")]
    public async Task HandleAsync_WhenReadbackDigits_DoesNotMutateBuffer_AndSpeaks(string isoCode)
    {
        var spoken = new List<string>();
        var actionFlow = CreateActionFlow(spoken);
        var buffer = CreateBuffer("123", "45");

        await actionFlow.HandleAsync(
            DigitsWorkflowCommand.ReadbackDigits,
            buffer,
            CreateContext(isoCode),
            CreateField(),
            CreateSession(),
            CancellationToken.None);

        Assert.Equal("12345", buffer.GetDigits());
        Assert.Single(spoken);
        Assert.NotEqual(string.Empty, spoken[0].Trim());
    }

    [Fact]
    public async Task HandleAsync_WhenReadbackDigits_GermanReadback_NamesTheCapturedDigits()
    {
        var spoken = new List<string>();
        var actionFlow = CreateActionFlow(spoken);
        var buffer = CreateBuffer("123", "45");

        await actionFlow.HandleAsync(
            DigitsWorkflowCommand.ReadbackDigits,
            buffer,
            CreateContext("de"),
            CreateField(),
            CreateSession(),
            CancellationToken.None);

        // Der Ausgabetext trennt Ziffern durch Leerzeichen und Blöcke durch Kommas.
        // Der gespeicherte Wert bleibt ohne Trennzeichen; tatsächliche Sprechpausen werden nicht gemessen.
        Assert.Contains("1 2 3, 4 5", spoken[0]);
        Assert.Equal("12345", buffer.GetDigits());
    }

    [Theory]
    [InlineData("de")]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("it")]
    public async Task HandleAsync_WhenRedictateLastBlock_RemovesLastBlock_AndAnnounces(string isoCode)
    {
        var spoken = new List<string>();
        var actionFlow = CreateActionFlow(spoken);
        var buffer = CreateBuffer("123", "45");

        await actionFlow.HandleAsync(
            DigitsWorkflowCommand.RedictateLastBlock,
            buffer,
            CreateContext(isoCode),
            CreateField(),
            CreateSession(),
            CancellationToken.None);

        Assert.Equal("123", buffer.GetDigits());
        Assert.Single(spoken);
        Assert.NotEqual(string.Empty, spoken[0].Trim());
    }

    [Fact]
    public async Task HandleAsync_WhenRedictateLastBlock_GermanPrompt_AnnouncesTheDeletion()
    {
        var spoken = new List<string>();
        var actionFlow = CreateActionFlow(spoken);
        var buffer = CreateBuffer("123", "45");

        await actionFlow.HandleAsync(
            DigitsWorkflowCommand.RedictateLastBlock,
            buffer,
            CreateContext("de"),
            CreateField(),
            CreateSession(),
            CancellationToken.None);

        // Der Hinweis nennt die Löschung, damit die Änderung ohne Sichtkontrolle verständlich ist.
        Assert.Contains("gelöscht", spoken[0]);
    }

    [Theory]
    [InlineData(DigitsWorkflowCommand.RemoveLastDigit, "1234")]
    [InlineData(DigitsWorkflowCommand.RemoveLastBlock, "123")]
    [InlineData(DigitsWorkflowCommand.RedictateLastBlock, "123")]
    public async Task HandleAsync_EveryDestructiveCommand_MutatesBuffer_AndSpeaks(
        DigitsWorkflowCommand command,
        string expectedDigits)
    {
        var spoken = new List<string>();
        var actionFlow = CreateActionFlow(spoken);
        var buffer = CreateBuffer("123", "45");

        await actionFlow.HandleAsync(
            command,
            buffer,
            CreateContext("de"),
            CreateField(),
            CreateSession(),
            CancellationToken.None);

        Assert.Equal(expectedDigits, buffer.GetDigits());
        Assert.Single(spoken);
    }

    private static DigitsWorkflowCommandActionFlow CreateActionFlow(List<string> spoken)
    {
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Loose);
        speechService
            .Setup(s => s.SpeakSafeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, string, VoiceDialogSettings, CancellationToken>((text, _, _, _) =>
            {
                spoken.Add(text);
                return Task.CompletedTask;
            });
        speechService
            .Setup(s => s.CaptureTranscriptAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, string, FormVoiceCommandCaptureSettings, Action, SpeechCaptureTelemetryContext?, RecognitionProfile, SpeechKeyterms?, CancellationToken>(
                (_, _, _, onStartListening, _, _, _, _) =>
                {
                    onStartListening();
                    return Task.FromResult<SpeechCaptureResult>(new SpeechCaptureResult("weiter", null));
                });

        return new DigitsWorkflowCommandActionFlow(
            new Mock<IEvaluationTelemetry>(MockBehavior.Loose).Object,
            speechService.Object,
            new DigitsWorkflowNoValuePrompt(speechService.Object));
    }

    private static DigitsWorkflowBuffer CreateBuffer(params string[] blocks)
    {
        var buffer = new DigitsWorkflowBuffer();
        foreach (var block in blocks)
        {
            buffer.Append(block, confidencePercent: null);
        }

        return buffer;
    }

    private static EvaluationSession CreateSession()
        => new("test-session", EvaluationChannel.Voice, "test-app", "form-1");

    private static FormFieldSnapshot CreateField()
        => new(
            Identity: new FormFieldIdentitySnapshot(FieldId: "name", VariableId: "name", FieldGroupId: "main"),
            Presentation: new FormFieldPresentationSnapshot(
                FieldGroupTitle: "Main",
                Label: "Name",
                HelpText: null,
                Examples: [],
                Options: []),
            Interaction: new FormFieldInteractionSnapshot(
                CodeStyle: false,
                InputMode: InputMode.Digits,
                Editability: EditabilityModes.UserEditable,
                ValueTypeKind: ValueTypeKinds.String,
                SpeakValueInConfirmation: true));

    private static FormVoiceCommandContext CreateContext(string isoCode)
    {
        var form = new FormDescriptor
        {
            FormId = FormId.From("current"),
            DisplayName = new LocalizedText { ["de"] = "Teil 1" },
            Description = new LocalizedText { ["de"] = "Teil 1" },
            LatestContractVersion = "1",
            AvailableContractVersions = ["1"]
        };

        var application = new ApplicationDescriptor
        {
            ApplicationId = "app",
            DisplayName = new LocalizedText { ["de"] = "App" },
            Description = new LocalizedText { ["de"] = "App" },
            SupportedLanguages = ["de", "en", "fr", "es", "it"],
            DefaultLanguage = "de",
            Forms = [form]
        };

        return new FormVoiceCommandContext(
            SessionIdentity: new FormVoiceCommandSessionIdentity(
                IsoCode: isoCode,
                ApplicationId: "app",
                FormId: "current",
                WakeWordActivationForms: []),
            Selection: new FormVoiceCommandSelectionContext(
                VoiceSettings: new VoiceDialogSettings(),
                Application: application,
                CompletedFormIds: ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase),
                Form: form,
                Contract: CreateContract()),
            Values: new FormVoiceCommandValueSnapshot(
                ValuesByVariableId: ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
                SharedAdoptions: []));
    }

    private static FormContract CreateContract()
        => new()
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "app",
            FormId = FormId.From("current"),
            DisplayName = new LocalizedText { ["de"] = "Teil 1" },
            SupportedLanguages = ["de", "en", "fr", "es", "it"],
            DefaultLanguage = "de",
            StrictLocalization = false,
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
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("name") }
                    ]
                }
            ]
        };
}
