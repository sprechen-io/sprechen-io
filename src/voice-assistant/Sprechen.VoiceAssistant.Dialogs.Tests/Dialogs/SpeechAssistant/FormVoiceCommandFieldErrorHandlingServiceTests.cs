using Moq;
using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormVoiceCommandFieldErrorHandlingServiceTests
{
    [Fact]
    public async Task HandleCaptureFailureAsync_TracksTelemetryAndReturnsContinue()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        var service = new FormVoiceCommandFieldErrorHandlingService(telemetry.Object, speechService.Object, new FormVoiceCommandSpokenOutputReplayBuffer(), new CaptureReplayPolicy());
        var session = new EvaluationSession("session-1", EvaluationChannel.Voice, "app", "app/form");
        var field = CreateFieldSnapshot();
        var autoHelp = CreateAutoHelpTracker();
        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);

        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.Prompting));
        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.Listening));
        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.Prompting));
        telemetry.Setup(t => t.TrackFieldAttempt(session, field.FieldId, InputModes.Voice, false, null));
        telemetry.Setup(t => t.TrackConstraintViolation(session, field.FieldId, FormVoiceCommandFieldErrorCodes.NoInput, SeverityLevels.Warning, TelemetryErrorTriggers.SystemRecognition));
        telemetry.Setup(t => t.TrackRepairAttempt(session, field.FieldId, FormVoiceCommandFieldErrorCodes.NoInput));

        stateTracker.BeginFieldPrompt(field.FieldGroupId);
        stateTracker.BeginListening();

        var result = await service.HandleCaptureFailureAsync(
            session,
            field,
            stateTracker,
            InputModes.Voice,
            FormVoiceCommandFieldErrorCodes.NoInput,
            autoHelp,
            CancellationToken.None);

        Assert.True(result.ShouldContinue);
        Assert.False(result.WasSubmitted);
        Assert.Null(result.DialogResult);
        telemetry.VerifyAll();
        speechService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandlePromptedFailureAsync_TracksStateAndSpeaksPrompt()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        var service = new FormVoiceCommandFieldErrorHandlingService(telemetry.Object, speechService.Object, new FormVoiceCommandSpokenOutputReplayBuffer(), new CaptureReplayPolicy());
        var session = new EvaluationSession("session-2", EvaluationChannel.Voice, "app", "app/form");
        var context = CreateContext();
        var field = CreateFieldSnapshot();
        var autoHelp = CreateAutoHelpTracker();
        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        const string prompt = "Bitte wiederholen.";

        telemetry.Setup(t => t.TrackFieldAttempt(session, field.FieldId, InputModes.Keyboard, false, null));
        telemetry.Setup(t => t.TrackConstraintViolation(session, field.FieldId, FormVoiceCommandFieldErrorCodes.KeyboardModeActive, SeverityLevels.Warning, TelemetryErrorTriggers.UserInput));
        telemetry.Setup(t => t.TrackRepairAttempt(session, field.FieldId, FormVoiceCommandFieldErrorCodes.KeyboardModeActive));
        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.Prompting));
        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.Listening));
        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.Interpreting));
        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.ErrorPrompt));
        speechService.Setup(s => s.SpeakSafeAsync(prompt, context.IsoCode, It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        stateTracker.BeginFieldPrompt(field.FieldGroupId);
        stateTracker.BeginListening();
        stateTracker.BeginInterpretation();

        var result = await service.HandlePromptedFailureAsync(
            context,
            session,
            field,
            stateTracker,
            autoHelp,
            FormVoiceCommandFieldErrorCodes.KeyboardModeActive,
            prompt,
            TelemetryErrorTriggers.UserInput,
            CancellationToken.None,
            trackFieldAttempt: true,
            expectedInputMode: InputModes.Keyboard);

        Assert.True(result.ShouldContinue);
        Assert.False(result.WasSubmitted);
        Assert.Null(result.DialogResult);
        telemetry.VerifyAll();
        speechService.VerifyAll();
    }

    [Theory]
    [InlineData(FormVoiceCommandFieldErrorCodes.LowSttConfidence)]
    [InlineData(FormVoiceCommandFieldErrorCodes.UnrecognizedInput)]
    [InlineData(FormVoiceCommandFieldErrorCodes.UnknownCommand)]
    public async Task HandlePromptedFailureAsync_GenuineRecognitionFailure_ArmsStructuralTeachingReteach(string issueCode)
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        var service = new FormVoiceCommandFieldErrorHandlingService(
            telemetry.Object,
            speechService.Object,
            new FormVoiceCommandSpokenOutputReplayBuffer(),
            new CaptureReplayPolicy());
        var session = new EvaluationSession($"session-{issueCode}", EvaluationChannel.Voice, "app", "app/form");
        var context = CreateContext();
        var field = CreateFieldSnapshot();
        var autoHelp = CreateAutoHelpTracker();
        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        const string prompt = "Bitte wiederholen.";

        speechService
            .Setup(s => s.SpeakSafeAsync(prompt, context.IsoCode, It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        stateTracker.BeginFieldPrompt(field.FieldGroupId);
        stateTracker.BeginListening();
        stateTracker.BeginInterpretation();

        await service.HandlePromptedFailureAsync(
            context,
            session,
            field,
            stateTracker,
            autoHelp,
            issueCode,
            prompt,
            TelemetryErrorTriggers.SystemRecognition,
            CancellationToken.None);

        Assert.True(stateTracker.ConsumeStructuralTeachingReteach(field.FieldId));
        speechService.VerifyAll();
    }

    [Theory]
    [InlineData(FormVoiceCommandFieldErrorCodes.KeyboardModeActive)]
    [InlineData(FormVoiceCommandFieldErrorCodes.VoiceInputForbidden)]
    public async Task HandlePromptedFailureAsync_PolicyBlock_DoesNotArmStructuralTeachingReteach(string issueCode)
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        var service = new FormVoiceCommandFieldErrorHandlingService(
            telemetry.Object,
            speechService.Object,
            new FormVoiceCommandSpokenOutputReplayBuffer(),
            new CaptureReplayPolicy());
        var session = new EvaluationSession($"session-{issueCode}", EvaluationChannel.Voice, "app", "app/form");
        var context = CreateContext();
        var field = CreateFieldSnapshot();
        var autoHelp = CreateAutoHelpTracker();
        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        const string prompt = "Bitte wiederholen.";

        speechService
            .Setup(s => s.SpeakSafeAsync(prompt, context.IsoCode, It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        stateTracker.BeginFieldPrompt(field.FieldGroupId);
        stateTracker.BeginListening();
        stateTracker.BeginInterpretation();

        await service.HandlePromptedFailureAsync(
            context,
            session,
            field,
            stateTracker,
            autoHelp,
            issueCode,
            prompt,
            TelemetryErrorTriggers.UserInput,
            CancellationToken.None);

        Assert.False(stateTracker.ConsumeStructuralTeachingReteach(field.FieldId));
        speechService.VerifyAll();
    }

    [Fact]
    public async Task HandleCaptureFailureAsync_NoInput_DoesNotArmStructuralTeachingReteach()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        var service = new FormVoiceCommandFieldErrorHandlingService(
            telemetry.Object,
            speechService.Object,
            new FormVoiceCommandSpokenOutputReplayBuffer(),
            new CaptureReplayPolicy());
        var session = new EvaluationSession("session-no-input", EvaluationChannel.Voice, "app", "app/form");
        var field = CreateFieldSnapshot();
        var autoHelp = CreateAutoHelpTracker();
        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);

        stateTracker.BeginFieldPrompt(field.FieldGroupId);
        stateTracker.BeginListening();

        await service.HandleCaptureFailureAsync(
            session,
            field,
            stateTracker,
            InputModes.Voice,
            FormVoiceCommandFieldErrorCodes.NoInput,
            autoHelp,
            CancellationToken.None);

        Assert.False(stateTracker.ConsumeStructuralTeachingReteach(field.FieldId));
        speechService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleValidationViolationAsync_DoesNotArmStructuralTeachingReteach()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        var service = new FormVoiceCommandFieldErrorHandlingService(
            telemetry.Object,
            speechService.Object,
            new FormVoiceCommandSpokenOutputReplayBuffer(),
            new CaptureReplayPolicy());
        var session = new EvaluationSession("session-validation", EvaluationChannel.Voice, "app", "app/form");
        var context = CreateContext();
        var field = CreateFieldSnapshot();
        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        const string violationPrompt = "Das Format ist ungültig.";

        speechService
            .Setup(s => s.SpeakSafeAsync(violationPrompt, context.IsoCode, It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        stateTracker.BeginFieldPrompt(field.FieldGroupId);
        stateTracker.BeginListening();
        stateTracker.BeginInterpretation();
        stateTracker.BeginValidation();

        await service.HandleValidationViolationAsync(
            context,
            session,
            field,
            stateTracker,
            "form.invalid",
            violationPrompt,
            CancellationToken.None);

        Assert.False(stateTracker.ConsumeStructuralTeachingReteach(field.FieldId));
        speechService.VerifyAll();
    }

    [Fact]
    public async Task HandleManualCorrectionRequestedAsync_TracksRepairAndSpeaksCorrectionPrompt()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        var service = new FormVoiceCommandFieldErrorHandlingService(telemetry.Object, speechService.Object, new FormVoiceCommandSpokenOutputReplayBuffer(), new CaptureReplayPolicy());
        var session = new EvaluationSession("session-3", EvaluationChannel.Voice, "app", "app/form");
        var context = CreateContext();
        var field = CreateFieldSnapshot();
        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);

        telemetry.Setup(t => t.TrackRepairAttempt(session, field.FieldId, FormVoiceCommandFieldErrorCodes.ManualCorrection));
        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.Prompting));
        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.Listening));
        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.Interpreting));
        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.Prompting));
        speechService
            .Setup(s => s.SpeakSafeAsync(It.IsAny<string>(), context.IsoCode, It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        stateTracker.BeginFieldPrompt(field.FieldGroupId);
        stateTracker.BeginListening();
        stateTracker.BeginInterpretation();

        var result = await service.HandleManualCorrectionRequestedAsync(
            context,
            session,
            field,
            stateTracker,
            CancellationToken.None);

        Assert.True(result.ShouldContinue);
        Assert.False(result.WasSubmitted);
        Assert.Null(result.DialogResult);
        telemetry.VerifyAll();
        speechService.VerifyAll();
    }

    [Fact]
    public async Task HandleValidationViolationAsync_TracksRepairAndSpeaksViolationPrompt()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        var service = new FormVoiceCommandFieldErrorHandlingService(telemetry.Object, speechService.Object, new FormVoiceCommandSpokenOutputReplayBuffer(), new CaptureReplayPolicy());
        var session = new EvaluationSession("session-4", EvaluationChannel.Voice, "app", "app/form");
        var context = CreateContext();
        var field = CreateFieldSnapshot();
        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        const string violationPrompt = "Das Format ist ungültig.";

        telemetry.Setup(t => t.TrackRepairAttempt(session, field.FieldId, "form.invalid"));
        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.Prompting));
        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.Listening));
        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.Interpreting));
        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.Validating));
        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.ErrorPrompt));
        speechService
            .Setup(s => s.SpeakSafeAsync(violationPrompt, context.IsoCode, It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        stateTracker.BeginFieldPrompt(field.FieldGroupId);
        stateTracker.BeginListening();
        stateTracker.BeginInterpretation();
        stateTracker.BeginValidation();

        var result = await service.HandleValidationViolationAsync(
            context,
            session,
            field,
            stateTracker,
            "form.invalid",
            violationPrompt,
            CancellationToken.None);

        Assert.True(result.ShouldContinue);
        Assert.False(result.WasSubmitted);
        Assert.Null(result.DialogResult);
        telemetry.VerifyAll();
        speechService.VerifyAll();
    }

    [Fact]
    public async Task HandleCaptureFailureAsync_Manner_ReVoicesTheCurrentFieldAnnouncementOnceWithTheLeadInMarker()
    {
        var telemetry = new Mock<IEvaluationTelemetry>();
        var speechService = new RecordingSpeechService();
        var replayBuffer = new FormVoiceCommandSpokenOutputReplayBuffer();
        var service = new FormVoiceCommandFieldErrorHandlingService(
            telemetry.Object, speechService, replayBuffer, new CaptureReplayPolicy());
        var session = new EvaluationSession("session-replay", EvaluationChannel.Voice, "app", "app/form");
        var field = CreateFieldSnapshot();
        var autoHelp = CreateAutoHelpTracker();
        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        stateTracker.BeginFieldPrompt(field.FieldGroupId);
        stateTracker.BeginListening();

        // Der Test hinterlegt eine wiederholbare Ansage für diesen Felddurchgang.
        replayBuffer.BeginFieldTurn(field.FieldId);
        replayBuffer.Record("Das Datum muss in der Vergangenheit liegen.");

        await service.HandleCaptureFailureAsync(
            session,
            field,
            stateTracker,
            InputModes.Voice,
            FormVoiceCommandFieldErrorCodes.NoSpeechDetected,
            autoHelp,
            CancellationToken.None,
            autoReplayEligible: true);

        var replay = Assert.Single(speechService.ReplayCalls);
        Assert.Equal("Noch einmal. Das Datum muss in der Vergangenheit liegen.", replay.Text);
        Assert.Equal("de", replay.IsoCode);
        // Nach der Wiederholung ist die gespeicherte Ansage verbraucht.
        Assert.Null(replayBuffer.CurrentFieldTurnAnnouncement);
    }

    [Fact]
    public async Task HandleCaptureFailureAsync_Quantity_DoesNotStackASecondReVoiceOnTheNextSilence()
    {
        var telemetry = new Mock<IEvaluationTelemetry>();
        var speechService = new RecordingSpeechService();
        var replayBuffer = new FormVoiceCommandSpokenOutputReplayBuffer();
        var service = new FormVoiceCommandFieldErrorHandlingService(
            telemetry.Object, speechService, replayBuffer, new CaptureReplayPolicy());
        var session = new EvaluationSession("session-cap", EvaluationChannel.Voice, "app", "app/form");
        var field = CreateFieldSnapshot();
        var autoHelp = CreateAutoHelpTracker();
        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        stateTracker.BeginFieldPrompt(field.FieldGroupId);
        stateTracker.BeginListening();
        replayBuffer.BeginFieldTurn(field.FieldId);
        replayBuffer.Record("Bitte geben Sie eine gueltige E-Mail an.");

        await service.HandleCaptureFailureAsync(
            session, field, stateTracker, InputModes.Voice, FormVoiceCommandFieldErrorCodes.NoSpeechDetected, autoHelp, CancellationToken.None, autoReplayEligible: true);
        await service.HandleCaptureFailureAsync(
            session, field, stateTracker, InputModes.Voice, FormVoiceCommandFieldErrorCodes.NoSpeechDetected, autoHelp, CancellationToken.None, autoReplayEligible: true);

        Assert.Single(speechService.ReplayCalls);
    }

    [Fact]
    public async Task HandleCaptureFailureAsync_DeadInputTerminal_DoesNotReVoice()
    {
        var telemetry = new Mock<IEvaluationTelemetry>();
        var speechService = new RecordingSpeechService();
        var replayBuffer = new FormVoiceCommandSpokenOutputReplayBuffer();
        var service = new FormVoiceCommandFieldErrorHandlingService(
            telemetry.Object, speechService, replayBuffer, new CaptureReplayPolicy());
        var session = new EvaluationSession("session-deadmic", EvaluationChannel.Voice, "app", "app/form");
        var field = CreateFieldSnapshot();
        var autoHelp = CreateAutoHelpTracker();
        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        stateTracker.BeginFieldPrompt(field.FieldGroupId);
        stateTracker.BeginListening();
        replayBuffer.BeginFieldTurn(field.FieldId);
        replayBuffer.Record("Das Datum muss in der Vergangenheit liegen.");

        // Bei NoInput ohne Freigabe zur automatischen Wiederholung bleibt die Ansage aus.
        await service.HandleCaptureFailureAsync(
            session,
            field,
            stateTracker,
            InputModes.Voice,
            FormVoiceCommandFieldErrorCodes.NoInput,
            autoHelp,
            CancellationToken.None,
            autoReplayEligible: false);

        Assert.Empty(speechService.ReplayCalls);
    }

    [Fact]
    public async Task HandleCaptureFailureAsync_WhenNoFreshAnnouncement_DoesNothingBeyondTheExistingPath()
    {
        var telemetry = new Mock<IEvaluationTelemetry>();
        var speechService = new RecordingSpeechService();
        var replayBuffer = new FormVoiceCommandSpokenOutputReplayBuffer();
        var service = new FormVoiceCommandFieldErrorHandlingService(
            telemetry.Object, speechService, replayBuffer, new CaptureReplayPolicy());
        var session = new EvaluationSession("session-none", EvaluationChannel.Voice, "app", "app/form");
        var field = CreateFieldSnapshot();
        var autoHelp = CreateAutoHelpTracker();
        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        stateTracker.BeginFieldPrompt(field.FieldGroupId);
        stateTracker.BeginListening();

        // Für diesen Felddurchgang ist keine wiederholbare Ansage gespeichert.
        replayBuffer.BeginFieldTurn(field.FieldId);

        await service.HandleCaptureFailureAsync(
            session, field, stateTracker, InputModes.Voice, FormVoiceCommandFieldErrorCodes.NoSpeechDetected, autoHelp, CancellationToken.None, autoReplayEligible: true);

        Assert.Empty(speechService.ReplayCalls);
    }

    private static FormVoiceCommandAutoHelpTracker CreateAutoHelpTracker()
    {
        var promptService = new Mock<IFormVoiceCommandPromptService>(MockBehavior.Strict);
        var interpreter = new Mock<IFormDialogRuntime>(MockBehavior.Strict);

        return new FormVoiceCommandAutoHelpTracker(
            new VoiceDialogSettings { MaxRetries = 0 },
            promptService.Object,
            new Mock<IFormVoiceCommandDialogStateTracker>().Object,
            interpreter.Object,
            isoCode: "de");
    }

    private static FormFieldSnapshot CreateFieldSnapshot()
    {
        return new FormFieldSnapshot(
            Identity: new FormFieldIdentitySnapshot(
                FieldId: "name",
                VariableId: "name",
                FieldGroupId: "main"),
            Presentation: new FormFieldPresentationSnapshot(
                FieldGroupTitle: "Main",
                Label: "Name",
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

    private static FormVoiceCommandContext CreateContext()
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

        var contract = new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "app",
            FormId = FormId.From("current"),
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
