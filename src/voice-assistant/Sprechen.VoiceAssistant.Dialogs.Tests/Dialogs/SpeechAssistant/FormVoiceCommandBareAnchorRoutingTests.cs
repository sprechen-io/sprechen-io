using System.Collections.Immutable;
using System.Text.Json;
using Moq;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

/// <summary>
/// Prüft die Behandlung eines alleinstehenden Aktivierungsworts im Wertefeld.
/// Der Zweig soll eine erneute Eingabe ermöglichen, eine Rückmeldung anfordern
/// und keinen Text zur Feldübernahme weitergeben.
/// </summary>
public sealed class FormVoiceCommandBareAnchorRoutingTests
{
    [Theory]
    [InlineData("okay guide")]
    [InlineData("okayguide")]
    public async Task ResolveAsync_ForBareCommandAnchor_StaysFieldLocal_ReprompsNeverCommitsNeverAborts(string transcript)
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(s => s.SpeakSafeAsync(It.IsAny<string>(), "de", It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);

        var branch = new FormVoiceCommandAmbiguityTranscriptBranch(
            new FormVoiceCommandAmbiguityResolver(),
            new FormVoiceCommandFieldErrorHandlingService(telemetry.Object, speechService.Object, new FormVoiceCommandSpokenOutputReplayBuffer(), new CaptureReplayPolicy()),
            new FormVoiceCommandInputVsCommandTelemetry(
                telemetry.Object,
                new InputVsCommandResolutionStrategyMapper()));

        var turnContext = CreateTurnContext(telemetry.Object, anchorForms: ["okay guide"]);

        var result = await branch.ResolveAsync(
            turnContext,
            CreateInterpretation(transcript),
            sttConfidencePercent: 95,
            CancellationToken.None);

        Assert.True(result.HasFinalResult);

        var handling = result.FinalResult!;
        Assert.True(handling.ShouldContinue);
        Assert.False(handling.WasSubmitted);
        Assert.Null(handling.DialogResult);

        // Keinen Text zur Feldübernahme und keinen Annahmekontext weitergeben.
        Assert.Null(result.TranscriptForApply);
        Assert.Null(result.AmbiguityTelemetry);

        // Fehlgeschlagene Eingabe protokollieren und eine Rückmeldung anfordern.
        telemetry.Verify(
            t => t.TrackConstraintViolation(
                turnContext.Session,
                turnContext.Field.FieldId,
                FormVoiceCommandFieldErrorCodes.UnrecognizedInput,
                SeverityLevels.Warning,
                TelemetryErrorTriggers.SystemRecognition),
            Times.Once);
        speechService.Verify(
            s => s.SpeakSafeAsync(It.IsAny<string>(), "de", It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()),
            Times.Once);

        router.VerifyNoOtherCalls();
    }

    private static FormVoiceCommandTranscriptInterpretation CreateInterpretation(string transcript)
        => new(
            new FormVoiceCommandTranscriptSource(
                TrimmedTranscript: transcript,
                InputTranscript: transcript,
                HasExplicitCommandPrefix: false),
            new FormVoiceCommandParsedCommand(
                HasParsedCommand: false,
                ParsedCommandId: string.Empty,
                ParsedCommandArgument: null,
                CommandSignalClass: CommandSignalClass.NoCommand),
            new FormVoiceCommandInterpretationMode(
                IsTextField: true,
                IsLiteralMode: false,
                IsDigitsMode: false,
                ShouldExecuteParsedCommand: false,
                ShouldExecuteParsedCommandInDigitsMode: false));

    private static FormVoiceCommandFieldTurnContext CreateTurnContext(
        IEvaluationTelemetry telemetry,
        ImmutableArray<string> anchorForms)
    {
        var contract = CreateMinimalContract();
        var context = CreateContext(contract, anchorForms);
        var field = CreateFieldSnapshot();
        var session = new EvaluationSession("session-bare-anchor", EvaluationChannel.Voice, "app", "app/current");
        var stateTracker = new FormVoiceCommandStateTracker(telemetry, session);
        stateTracker.BeginFieldPrompt(field.FieldGroupId);
        stateTracker.BeginListening();
        stateTracker.BeginInterpretation();

        var interpreter = new Mock<IFormDialogRuntime>(MockBehavior.Strict);
        var autoHelp = new FormVoiceCommandAutoHelpTracker(
            new VoiceDialogSettings { MaxRetries = 0 },
            Mock.Of<IFormVoiceCommandPromptService>(),
            stateTracker,
            interpreter.Object,
            isoCode: "de");

        return new FormVoiceCommandFieldTurnContext(
            context,
            interpreter.Object,
            field,
            session,
            stateTracker,
            autoHelp,
            new FormVoiceCommandUnrecognizedInputEscalationPolicy(),
            Mock.Of<IFormDialogPrivacyReadbackPolicy>());
    }

    private static FormFieldSnapshot CreateFieldSnapshot()
        => new(
            new FormFieldIdentitySnapshot("name", "name", "main"),
            new FormFieldPresentationSnapshot(
                FieldGroupTitle: "Main",
                Label: "Name",
                HelpText: null,
                Examples: [],
                Options: []),
            new FormFieldInteractionSnapshot(
                CodeStyle: false,
                InputMode: InputMode.Voice,
                Editability: EditabilityModes.UserEditable,
                ValueTypeKind: "string",
                SpeakValueInConfirmation: true));

    private static FormContract CreateMinimalContract()
        => new()
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "app",
            FormId = FormId.From("current"),
            DisplayName = new LocalizedText { ["de"] = "Teil 1" },
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

    private static FormVoiceCommandContext CreateContext(FormContract contract, ImmutableArray<string> anchorForms)
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
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            Forms = [form]
        };

        return new FormVoiceCommandContext(
            SessionIdentity: new FormVoiceCommandSessionIdentity(
                IsoCode: "de",
                ApplicationId: "app",
                FormId: "current",
                WakeWordActivationForms: anchorForms),
            Selection: new FormVoiceCommandSelectionContext(
                VoiceSettings: new VoiceDialogSettings { MaxRetries = 0 },
                Application: application,
                CompletedFormIds: ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase),
                Form: form,
                Contract: contract),
            Values: new FormVoiceCommandValueSnapshot(
                ValuesByVariableId: ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
                SharedAdoptions: []));
    }
}
