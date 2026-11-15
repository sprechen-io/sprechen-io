using Moq;
using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

// Prüft die oneOf-Validierung mit dem echten Formularinterpreter und die Übergabe des Fehlers an den Rückfragedialog.
// Die Sprachausgabe wird simuliert; geprüft werden der angeforderte Text und die Telemetrieaufrufe.
public sealed class FormVoiceCommandFieldApplyValidationRejectionTests
{
    [Fact]
    public async Task TryApplyTranscript_WhenOneOfValueOutOfSet_ProducesViolationThroughRealApplyPath()
    {
        var contract = CreateOneOfContract();
        var interpreter = new FormInterpreterRuntimeOwner(contract, "de", ImmutableDictionary<string, string>.Empty);

        // „Gelb“ ist als Text lesbar, gehört aber nicht zu den drei erlaubten Farben.
        var inputResult = await interpreter.TryApplyTranscriptAsync("Gelb");

        Assert.True(inputResult.ValueCaptured);
        var violation = Assert.Single(inputResult.Violations);
        Assert.Equal("colorOneOf", violation.ConstraintId);
        Assert.Equal("colorField", violation.FieldId);
    }

    [Fact]
    public async Task ApplyAsync_WhenOneOfValueOutOfSet_ReturnsContinueAndSpeaksViolationPrompt()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        var spokenPrompts = new List<string>();
        speechService
            .Setup(s => s.SpeakSafeAsync(It.IsAny<string>(), "de", It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
            .Returns<string, string, VoiceDialogSettings, CancellationToken>((prompt, _, _, _) =>
            {
                spokenPrompts.Add(prompt);
                return Task.CompletedTask;
            });

        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        var submissionService = new Mock<IFormVoiceCommandSubmissionService>(MockBehavior.Strict);
        var service = CreateService(telemetry.Object, speechService.Object, router.Object, submissionService.Object);

        var contract = CreateOneOfContract();
        var context = CreateContext(contract);
        var field = CreateColorFieldSnapshot();
        var session = new EvaluationSession("validation-reject-session", EvaluationChannel.Voice, "app", "app/current");

        var interpreter = new FormInterpreterRuntimeOwner(contract, "de", ImmutableDictionary<string, string>.Empty);
        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        PrepareTrackerForApply(stateTracker, field.FieldGroupId);
        var autoHelp = CreateAutoHelpTracker(interpreter);

        var result = await service.ApplyAsync(
            context,
            interpreter,
            field,
            session,
            stateTracker,
            autoHelp,
            expectedInputMode: InputMode.Voice,
            transcriptForApply: "Gelb",
            ambiguityTelemetry: null,
            sttConfidencePercent: null,
            CancellationToken.None);

        Assert.True(result.ShouldContinue);
        Assert.False(result.WasSubmitted);
        Assert.Null(result.DialogResult);

        telemetry.Verify(
            t => t.TrackConstraintViolation(
                session,
                "colorField",
                "form.oneOf.invalid",
                SeverityLevels.Error,
                It.IsAny<string>(),
                It.IsAny<string?>(),
                TelemetryErrorTriggers.UserInput),
            Times.Once);

        var violationPrompt = Assert.Single(spokenPrompts);
        Assert.Contains("Farbe", violationPrompt, StringComparison.Ordinal);

        router.VerifyNoOtherCalls();
        submissionService.VerifyNoOtherCalls();
    }

    private static FormContract CreateOneOfContract()
    {
        var stringValueType = JsonDocument.Parse("{\"kind\":\"string\"}").RootElement;

        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "app",
            FormId = FormId.From("current"),
            DisplayName = new LocalizedText { ["de"] = "Teil 1" },
            DefaultLanguage = "de",
            SupportedLanguages = ["de"],
            EntryFieldGroupId = FieldGroupId.From("main"),
            StrictLocalization = true,
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("color"),
                    Name = "color",
                    ValueType = stringValueType
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("colorField"),
                    VariableId = VariableId.From("color"),
                    Label = new LocalizedText { ["de"] = "Farbe" },
                    Editability = FieldEditability.UserEditable
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("main"),
                    Title = new LocalizedText { ["de"] = "Main" },
                    Members = [new FieldGroupMember { Kind = MemberKinds.Field, FieldId = FieldId.From("colorField") }]
                }
            ],
            Constraints =
            [
                new ConstraintDefinition
                {
                    ConstraintId = "colorOneOf",
                    Target = new ConstraintTargetRef { Kind = TargetKinds.Field, FieldId = FieldId.From("colorField") },
                    Severity = ConstraintSeverity.Error,
                    Code = "form.oneOf.invalid",
                    Message = new LocalizedText { ["de"] = "Diese Farbe ist nicht zulässig." },
                    Remediation = new LocalizedText { ["de"] = "Bitte Rot, Grün oder Blau nennen." },
                    Kind = ConstraintKind.OneOf,
                    AdditionalProperties = new Dictionary<string, JsonElement>
                    {
                        ["values"] = JsonDocument.Parse("[\"Rot\",\"Gr\\u00fcn\",\"Blau\"]").RootElement
                    }
                }
            ]
        };
    }

    private static FormVoiceCommandFieldApplyService CreateService(
        IEvaluationTelemetry telemetry,
        IFormVoiceCommandSpeechService speechService,
        IFormVoiceCommandGlobalCommandRouter commandRouter,
        IFormVoiceCommandSubmissionService submissionService)
    {
        var commandExecutionService = new FormVoiceCommandGlobalCommandExecutionService(telemetry, commandRouter);
        var confirmationDecisionService = CreateConfirmationDecisionService(speechService, new FormTranscriptParser(new YesNoTranscriptClassifier(), new ModeledNegationParticleResolver()));
        var earcon = Mock.Of<IEarconPlayer>();
        var presenter = new FormVoiceCommandConfirmationValuePresenter(
            speechService,
            new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance));
        var errorHandlingService = new FormVoiceCommandFieldErrorHandlingService(telemetry, speechService, new FormVoiceCommandSpokenOutputReplayBuffer(), new CaptureReplayPolicy());
        var confirmationHandler = new FormVoiceCommandConfirmationHandler(
            presenter,
            confirmationDecisionService,
            new FormVoiceCommandConfirmationRepairActionFlow(telemetry, speechService, commandExecutionService),
            new FormVoiceCommandConfirmedFieldCompletionFlow(
                speechService,
                earcon,
                new FormClosingReviewTurnFlow(speechService, commandExecutionService)),
            errorHandlingService);
        var spellingEscalationFlow = new FormVoiceCommandSpellingEscalationFlow(
            new SpellingConfidenceEscalationPolicy(),
            presenter,
            confirmationDecisionService,
            new FormVoiceCommandConfirmationRepairActionFlow(telemetry, speechService, commandExecutionService),
            Mock.Of<IEarconPlayer>());

        return new FormVoiceCommandFieldApplyService(
            telemetry,
            telemetry,
            errorHandlingService,
            confirmationHandler,
            presenter,
            new FormVoiceCommandAdditionalFieldProposalHandler(
                telemetry,
                new FormVoiceCommandConfirmationTranscriptCapture(speechService),
                new FormTranscriptParser(new YesNoTranscriptClassifier(), new ModeledNegationParticleResolver()),
                speechService),
            spellingEscalationFlow,
            new FormRepairReasonResolver());
    }

    private static FormVoiceCommandConfirmationDecisionService CreateConfirmationDecisionService(
        IFormVoiceCommandSpeechService speechService,
        IFormTranscriptParser transcriptParser)
        => new(
            new FormVoiceCommandConfirmationTranscriptCapture(speechService),
            new FormVoiceCommandConfirmationTranscriptInterpreter(
                new FormVoiceCommandConfirmationCommandDecisionMapper(),
                transcriptParser),
            new FormVoiceCommandConfirmationMissingDecisionPolicy());

    private static FormVoiceCommandAutoHelpTracker CreateAutoHelpTracker(IFormDialogRuntime interpreter)
    {
        var promptService = new Mock<IFormVoiceCommandPromptService>(MockBehavior.Strict);
        return new FormVoiceCommandAutoHelpTracker(
            new VoiceDialogSettings { MaxRetries = 0 },
            promptService.Object,
            new Mock<IFormVoiceCommandDialogStateTracker>().Object,
            interpreter,
            isoCode: "de");
    }

    private static void PrepareTrackerForApply(FormVoiceCommandStateTracker stateTracker, string fieldGroupId)
    {
        stateTracker.BeginFieldPrompt(fieldGroupId);
        stateTracker.BeginListening();
        stateTracker.BeginInterpretation();
    }

    private static FormFieldSnapshot CreateColorFieldSnapshot()
    {
        return new FormFieldSnapshot(
            Identity: new FormFieldIdentitySnapshot(
                FieldId: "colorField",
                VariableId: "color",
                FieldGroupId: "main"),
            Presentation: new FormFieldPresentationSnapshot(
                FieldGroupTitle: "Main",
                Label: "Farbe",
                HelpText: null,
                Examples: [],
                Options: []),
            Interaction: new FormFieldInteractionSnapshot(
                CodeStyle: false,
                InputMode: InputMode.Voice,
                Editability: EditabilityModes.UserEditable,
                ValueTypeKind: ValueTypeKinds.String,
                SpeakValueInConfirmation: true));
    }

    private static FormVoiceCommandContext CreateContext(FormContract contract)
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
                WakeWordActivationForms: []),
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
