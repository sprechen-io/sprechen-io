using Microsoft.Extensions.Logging.Abstractions;
using Sprechen.Speech.TestDoubles;
using Moq;
using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.FormSurfaces;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormVoiceCommandFieldInteractionHandlerTests
{
    private static readonly string[] SupportedEntryPromptIsoCodes = [.. LanguageDescriptors.AllIsoCodes];

    [Fact]
    public async Task HandleAsync_WhenEnteringFieldGroupWithVoiceSummary_SpeaksTitleIntroThenVoiceSummaryBeforeCapture()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");

        telemetry
            .Setup(t => t.TrackStateEntry(session, It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackFieldFocus(session, "name"));
        telemetry
            .Setup(t => t.TrackStatusSnapshot(session, It.IsAny<EvaluationFormStatusSnapshot>()));
        telemetry
            .Setup(t => t.TrackFieldAndGroupStatus(session, It.IsAny<EvaluationDetailedFormStatus>()));
        telemetry
            .Setup(t => t.TrackGlobalCommand(session, GlobalCommandIds.Next));
        telemetry
            .Setup(t => t.TrackAmbiguityResolved(
                session,
                It.IsAny<string>(),
                It.IsAny<string>(),
                CommandSignalClass.Clear,
                It.IsAny<string>(),
                "name",
                GlobalCommandIds.Next));

        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        var sequence = new MockSequence();
        speechService.InSequence(sequence)
            .Setup(s => s.SpeakSafeAsync(
                "Feldgruppe main.",
                "de",
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        speechService.InSequence(sequence)
            .Setup(s => s.SpeakSafeAsync(
                "Orientierung fuer Hauptgruppe",
                "de",
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        speechService.InSequence(sequence)
            .Setup(s => s.CaptureTranscriptAsync(
                It.IsAny<string>(),
                "de",
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, string, FormVoiceCommandCaptureSettings, Action, SpeechCaptureTelemetryContext?, RecognitionProfile, SpeechKeyterms?, CancellationToken>((_, _, _, onStartListening, _, _, _, _) =>
            {
                onStartListening();
                return Task.FromResult<SpeechCaptureResult>(new SpeechCaptureResult("okay weiter", null));
            });

        var submissionService = new Mock<IFormVoiceCommandSubmissionService>(MockBehavior.Strict);
        var expectedResult = VoiceCommandDialogResult.BackToFormSelection(
            "app",
            "current",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));

        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        router
            .Setup(r => r.TryHandleAsync(
                It.Is<CommandId>(c => c.Value == "submit"),
                It.IsAny<FormVoiceCommandGlobalCommandContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(VoiceCommandDialogResult.Submitted(
                "app",
                "current",
                ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase)));
        router
            .Setup(r => r.TryHandleAsync(
                CommandId.From(GlobalCommandIds.Next),
                It.IsAny<FormVoiceCommandGlobalCommandContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var handler = CreateFieldInteractionHandler(
            telemetry.Object,
            speechService.Object,
            submissionService.Object,
            router.Object);

        var contract = CreateMinimalContract();
        var interpreter = new FormInterpreterRuntimeOwner(
            contract,
            requestedIsoCode: "de",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));
        var context = CreateContext(contract) with
        {
            FormProjection = CreateFormProjection(("main", "Orientierung fuer Hauptgruppe"))
        };

        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        var promptService = new Mock<IFormVoiceCommandPromptService>(MockBehavior.Strict);
        var autoHelp = new FormVoiceCommandAutoHelpTracker(
            context.VoiceSettings,
            promptService.Object,
            stateTracker,
            interpreter,
            context.IsoCode);

        var result = await handler.HandleAsync(
            context,
            interpreter,
            (await interpreter.GetFocusedFieldAsync()),
            session,
            stateTracker,
            autoHelp,
            CancellationToken.None);

        Assert.NotNull(result.DialogResult);
        Assert.Equal(VoiceCommandNavigationKind.BackToFormSelection, result.DialogResult.Navigation);
        speechService.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenEnteringFieldGroupWithoutVoiceSummary_AnnouncesTitleIntroBeforeCapture()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");

        telemetry.Setup(t => t.TrackStateEntry(session, It.IsAny<string>()));
        telemetry.Setup(t => t.TrackFieldFocus(session, "name"));
        telemetry.Setup(t => t.TrackStatusSnapshot(session, It.IsAny<EvaluationFormStatusSnapshot>()));
        telemetry.Setup(t => t.TrackFieldAndGroupStatus(session, It.IsAny<EvaluationDetailedFormStatus>()));
        telemetry.Setup(t => t.TrackGlobalCommand(session, GlobalCommandIds.Next));
        telemetry.Setup(t => t.TrackAmbiguityResolved(
            session,
            It.IsAny<string>(),
            It.IsAny<string>(),
            CommandSignalClass.Clear,
            It.IsAny<string>(),
            "name",
            GlobalCommandIds.Next));

        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        var sequence = new MockSequence();
        speechService.InSequence(sequence)
            .Setup(s => s.SpeakSafeAsync(
                "Feldgruppe main.",
                "de",
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        speechService.InSequence(sequence)
            .Setup(s => s.CaptureTranscriptAsync(
                It.IsAny<string>(),
                "de",
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, string, FormVoiceCommandCaptureSettings, Action, SpeechCaptureTelemetryContext?, RecognitionProfile, SpeechKeyterms?, CancellationToken>((_, _, _, onStartListening, _, _, _, _) =>
            {
                onStartListening();
                return Task.FromResult<SpeechCaptureResult>(new SpeechCaptureResult("okay weiter", null));
            });

        var submissionService = new Mock<IFormVoiceCommandSubmissionService>(MockBehavior.Strict);
        var expectedResult = VoiceCommandDialogResult.BackToFormSelection(
            "app",
            "current",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));

        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        router
            .Setup(r => r.TryHandleAsync(
                It.Is<CommandId>(c => c.Value == "submit"),
                It.IsAny<FormVoiceCommandGlobalCommandContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(VoiceCommandDialogResult.Submitted(
                "app",
                "current",
                ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase)));
        router
            .Setup(r => r.TryHandleAsync(
                CommandId.From(GlobalCommandIds.Next),
                It.IsAny<FormVoiceCommandGlobalCommandContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var handler = CreateFieldInteractionHandler(
            telemetry.Object,
            speechService.Object,
            submissionService.Object,
            router.Object);

        var contract = CreateMinimalContract();
        var interpreter = new FormInterpreterRuntimeOwner(
            contract,
            requestedIsoCode: "de",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));
        var context = CreateContext(contract) with
        {
            FormProjection = CreateFormProjection(("main", null))
        };

        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        var promptService = new Mock<IFormVoiceCommandPromptService>(MockBehavior.Strict);
        var autoHelp = new FormVoiceCommandAutoHelpTracker(
            context.VoiceSettings,
            promptService.Object,
            stateTracker,
            interpreter,
            context.IsoCode);

        var result = await handler.HandleAsync(
            context,
            interpreter,
            (await interpreter.GetFocusedFieldAsync()),
            session,
            stateTracker,
            autoHelp,
            CancellationToken.None);

        Assert.NotNull(result.DialogResult);
        Assert.Equal(VoiceCommandNavigationKind.BackToFormSelection, result.DialogResult.Navigation);
        speechService.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenEnteringGuidancePairGroup_SpeaksGroupIntroThenNamePromptWithGuidance()
    {
        // Der Gruppentitel wird separat angefordert. Der Text vor der Aufnahme enthält anschließend die Anleitung; das schreibgeschützte Feld erhält keinen Eingabefokus.
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");

        telemetry.Setup(t => t.TrackStateEntry(session, It.IsAny<string>()));
        telemetry.Setup(t => t.TrackFieldFocus(session, "nameField"));
        telemetry.Setup(t => t.TrackStatusSnapshot(session, It.IsAny<EvaluationFormStatusSnapshot>()));
        telemetry.Setup(t => t.TrackFieldAndGroupStatus(session, It.IsAny<EvaluationDetailedFormStatus>()));
        telemetry.Setup(t => t.TrackGlobalCommand(session, GlobalCommandIds.Next));
        telemetry.Setup(t => t.TrackAmbiguityResolved(
            session,
            It.IsAny<string>(),
            It.IsAny<string>(),
            CommandSignalClass.Clear,
            It.IsAny<string>(),
            "nameField",
            GlobalCommandIds.Next));

        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        var sequence = new MockSequence();
        speechService.InSequence(sequence)
            .Setup(s => s.SpeakSafeAsync(
                "Feldgruppe Schritt 1: Vorname.",
                "de",
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        speechService.InSequence(sequence)
            .Setup(s => s.CaptureTranscriptAsync(
                It.Is<string>(prompt => prompt.EndsWith("Zur Information. Vorname. Der vorgegebene Wert lautet: Marta. ist Marta", StringComparison.Ordinal)),
                "de",
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, string, FormVoiceCommandCaptureSettings, Action, SpeechCaptureTelemetryContext?, RecognitionProfile, SpeechKeyterms?, CancellationToken>((_, _, _, onStartListening, _, _, _, _) =>
            {
                onStartListening();
                return Task.FromResult<SpeechCaptureResult>(new SpeechCaptureResult("okay weiter", null));
            });

        var submissionService = new Mock<IFormVoiceCommandSubmissionService>(MockBehavior.Strict);
        var expectedResult = VoiceCommandDialogResult.BackToFormSelection(
            "app",
            "current",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));

        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        router
            .Setup(r => r.TryHandleAsync(
                It.Is<CommandId>(c => c.Value == "submit"),
                It.IsAny<FormVoiceCommandGlobalCommandContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(VoiceCommandDialogResult.Submitted(
                "app",
                "current",
                ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase)));
        router
            .Setup(r => r.TryHandleAsync(
                CommandId.From(GlobalCommandIds.Next),
                It.IsAny<FormVoiceCommandGlobalCommandContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var handler = CreateFieldInteractionHandler(
            telemetry.Object,
            speechService.Object,
            submissionService.Object,
            router.Object);

        var contract = CreateGuidedNamePairContract();
        var interpreter = new FormInterpreterRuntimeOwner(
            contract,
            requestedIsoCode: "de",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));
        var context = CreateContext(contract) with
        {
            FormProjection = new FormSurfaceDefinition(
                "name",
                new Dictionary<string, FormSurfaceFieldGroupInfo>(StringComparer.OrdinalIgnoreCase)
                {
                    ["name"] = new("name", title: "Schritt 1: Vorname")
                },
                [])
        };

        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        var promptService = new Mock<IFormVoiceCommandPromptService>(MockBehavior.Strict);
        var autoHelp = new FormVoiceCommandAutoHelpTracker(
            context.VoiceSettings,
            promptService.Object,
            stateTracker,
            interpreter,
            context.IsoCode);

        var field = await interpreter.GetFocusedFieldAsync();

        // Schon vor dem Aufruf liegt der Fokus auf dem bearbeitbaren Namensfeld.
        Assert.Equal("nameField", field.FieldId);

        var result = await handler.HandleAsync(
            context,
            interpreter,
            field,
            session,
            stateTracker,
            autoHelp,
            CancellationToken.None);

        Assert.NotNull(result.DialogResult);
        Assert.Equal(VoiceCommandNavigationKind.BackToFormSelection, result.DialogResult.Navigation);
        telemetry.Verify(t => t.TrackFieldFocus(session, "guidanceNameField"), Times.Never);
        speechService.VerifyAll();
    }

    [Theory]
    [InlineData("enum", false)]
    [InlineData("enum", true)]
    [InlineData("set", false)]
    [InlineData("set", true)]
    public async Task HandleAsync_FirstChoiceTurn_PutsExistingGuidanceAfterOptions(string valueKind, bool allowBargeIn)
    {
        const string options = "Zur Auswahl stehen Marta und Lea.";
        const string guidance = "Zur Information. Vorname. Der vorgegebene Wert lautet: Marta. ist Marta";
        var contract = CreateGuidedNamePairContract();
        contract.Variables.Single(v => v.VariableId.Value == "name").ValueType = JsonDocument.Parse(
            $$$"""{"kind":"{{{valueKind}}}","values":[{"code":"marta","displayName":{"de":"Marta"}},{"code":"lea","displayName":{"de":"Lea"}}]}""").RootElement;
        contract.Fields.Single(f => f.FieldId.Value == "nameField").HelpText = new LocalizedText { ["de"] = options };
        var session = new EvaluationSession("choice-guidance", EvaluationChannel.Voice, "app", "current");
        var stateTracker = new FormVoiceCommandStateTracker(Mock.Of<IEvaluationTelemetry>(), session);

        var prompt = await EnterFieldAndCaptureEntryPromptAsync(
            contract, 0, session, stateTracker, allowBargeIn: allowBargeIn);

        Assert.EndsWith(guidance, prompt, StringComparison.Ordinal);
        Assert.Equal(1, prompt.Split(options, StringSplitOptions.None).Length - 1);
        Assert.Equal(1, prompt.Split(guidance, StringSplitOptions.None).Length - 1);
        Assert.True(prompt.IndexOf(options, StringComparison.Ordinal) < prompt.IndexOf(guidance, StringComparison.Ordinal));
        if (valueKind == "set")
        {
            Assert.Contains(SetStructureHintMarker, prompt, StringComparison.Ordinal);
            Assert.True(prompt.IndexOf(SetStructureHintMarker, StringComparison.Ordinal) < prompt.IndexOf(guidance, StringComparison.Ordinal));
        }
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public async Task HandleAsync_DeniedGuidedEntry_AnnouncesBeforeEveryDeniedOutcome(bool keyboardMode, bool canMoveNext)
    {
        const string guidance = "Zur Information. Vorname. Der vorgegebene Wert lautet: Marta. ist Marta";
        var contract = canMoveNext ? CreateTwoGuidedGroupsContract() : CreateGuidedNamePairContract();
        if (!keyboardMode)
        {
            var variable = contract.Variables.Single(v => v.VariableId.Value == "name");
            variable.PrivacyClass = PrivacyClass.Secret;
            variable.SensitiveDataPolicy = new SensitiveDataPolicy { SilentInputFallback = SensitiveDataPolicyDefaults.SilentInputFallbackKeyboard };
        }
        var interpreter = new FormInterpreterRuntimeOwner(contract, "de", ImmutableDictionary<string, string>.Empty);
        if (keyboardMode)
            Assert.True(await interpreter.TrySetFocusedFieldInputModeAsync(InputModes.Keyboard));
        var output = new List<(string Kind, string Text)>();
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService.Setup(x => x.SpeakSafeAsync(It.IsAny<string>(), "de", It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, VoiceDialogSettings, CancellationToken>((text, _, _, _) => output.Add(("announcement", text)))
            .Returns(Task.CompletedTask);
        speechService.Setup(x => x.CaptureConfirmationTranscriptAsync(It.IsAny<string>(), "de", It.IsAny<FormVoiceCommandCaptureSettings>(), It.IsAny<Action>(), It.IsAny<SpeechCaptureTelemetryContext?>(), It.IsAny<SpeechKeyterms?>(), It.IsAny<CancellationToken>()))
            .Returns<string, string, FormVoiceCommandCaptureSettings, Action, SpeechCaptureTelemetryContext?, SpeechKeyterms?, CancellationToken>((prompt, _, _, onStartListening, _, _, _) =>
            {
                output.Add(("command-capture", prompt));
                onStartListening();
                return Task.FromResult(new SpeechCaptureResult(null, null));
            });
        var telemetry = Mock.Of<IEvaluationTelemetry>();
        var handler = CreateFieldInteractionHandler(telemetry, speechService.Object, Mock.Of<IFormVoiceCommandSubmissionService>(), Mock.Of<IFormVoiceCommandGlobalCommandRouter>());
        var context = CreateContext(contract);
        var session = new EvaluationSession("denied-guidance", EvaluationChannel.Voice, "app", "current");
        var stateTracker = new FormVoiceCommandStateTracker(telemetry, session);
        var autoHelp = new FormVoiceCommandAutoHelpTracker(context.VoiceSettings, Mock.Of<IFormVoiceCommandPromptService>(), stateTracker, interpreter, "de");

        await handler.HandleAsync(context, interpreter, await interpreter.GetFocusedFieldAsync(), session, stateTracker, autoHelp, CancellationToken.None);

        var guidanceIndex = output.FindIndex(item => item.Text == guidance);
        Assert.True(guidanceIndex >= 0);
        Assert.Single(output, item => item.Text == guidance);
        var deniedOutputIndex = keyboardMode
            ? output.FindIndex(item => item.Kind == "command-capture")
            : output.FindIndex(item => item.Text == FormInputFeedbackPromptBuilder.GetVoiceInputForbiddenPrompt("de", "die Tastatur"));
        Assert.True(deniedOutputIndex > guidanceIndex);
        Assert.Equal(canMoveNext ? 0 : 1, output.Count(item => item.Kind == "command-capture"));
        Assert.Equal(canMoveNext ? "nextField" : "nameField", (await interpreter.GetFocusedFieldAsync()).FieldId);
        Assert.False((await interpreter.GetValuesSnapshotAsync()).ContainsKey("name"));
        Assert.True(await interpreter.TrySetFocusToFieldIdAsync("nameField"));
        output.Clear();

        await handler.HandleAsync(context, interpreter, await interpreter.GetFocusedFieldAsync(), session, stateTracker, autoHelp, CancellationToken.None);

        Assert.DoesNotContain(output, item => item.Text == guidance);
        speechService.Verify(x => x.CaptureTranscriptAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<FormVoiceCommandCaptureSettings>(), It.IsAny<Action>(), It.IsAny<SpeechCaptureTelemetryContext?>(), It.IsAny<RecognitionProfile>(), It.IsAny<SpeechKeyterms?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_GroupChangesAndReturnVisit_AnnounceEachGuidanceOnce()
    {
        var contract = CreateTwoGuidedGroupsContract();
        var session = new EvaluationSession("group-guidance", EvaluationChannel.Voice, "app", "current");
        var stateTracker = new FormVoiceCommandStateTracker(Mock.Of<IEvaluationTelemetry>(), session);

        var first = await EnterFieldAndCaptureEntryPromptAsync(contract, 0, session, stateTracker);
        var sameGroup = await EnterFieldAndCaptureEntryPromptAsync(contract, 0, session, stateTracker);
        var nextGroup = await EnterFieldAndCaptureEntryPromptAsync(contract, 1, session, stateTracker);
        var returned = await EnterFieldAndCaptureEntryPromptAsync(contract, 0, session, stateTracker);

        Assert.Contains("Zur Information.", first, StringComparison.Ordinal);
        Assert.DoesNotContain("Zur Information.", sameGroup, StringComparison.Ordinal);
        Assert.EndsWith("Zur Information. Zweiter Hinweis ist Lea", nextGroup, StringComparison.Ordinal);
        Assert.DoesNotContain("Zur Information.", returned, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_WhenSwitchFormCommandHasExplicitPrefix_UsesCanonicalCommandIdForRouting()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");

        telemetry
            .Setup(t => t.TrackStateEntry(session, It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackFieldFocus(session, It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackStatusSnapshot(session, It.IsAny<EvaluationFormStatusSnapshot>()));
        telemetry
            .Setup(t => t.TrackFieldAndGroupStatus(session, It.IsAny<EvaluationDetailedFormStatus>()));
        telemetry
            .Setup(t => t.TrackGlobalCommand(session, It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackAmbiguityResolved(
                session,
                It.IsAny<string>(),
                It.IsAny<string>(),
                CommandSignalClass.Clear,
                It.IsAny<string>(),
                It.IsAny<string>(),
                GlobalCommandIds.SwitchForm));

        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService.ForwardConfirmationCaptureToValueCapture();
        speechService
            .Setup(s => s.CaptureTranscriptAsync(
                It.IsAny<string>(),
                "de",
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, string, FormVoiceCommandCaptureSettings, Action, SpeechCaptureTelemetryContext?, RecognitionProfile, SpeechKeyterms?, CancellationToken>((_, _, _, onStartListening, _, _, _, _) =>
            {
                onStartListening();
                return Task.FromResult<SpeechCaptureResult>(new SpeechCaptureResult("kommando wechsle zu teil 2", null));
            });

        var submissionService = new Mock<IFormVoiceCommandSubmissionService>(MockBehavior.Strict);

        var expectedResult = VoiceCommandDialogResult.BackToFormSelection(
            "app",
            "current",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));

        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        router
            .Setup(r => r.TryHandleAsync(
                It.Is<CommandId>(c => c.Value == "submit"),
                It.IsAny<FormVoiceCommandGlobalCommandContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(VoiceCommandDialogResult.Submitted(
                "app",
                "current",
                ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase)));
        router
            .Setup(r => r.TryHandleAsync(
                CommandId.From(GlobalCommandIds.SwitchForm),
                It.IsAny<FormVoiceCommandGlobalCommandContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var handler = CreateFieldInteractionHandler(
            telemetry.Object,
            speechService.Object,
            submissionService.Object,
            router.Object);

        var contract = CreateMinimalContract();
        var interpreter = new FormInterpreterRuntimeOwner(
            contract,
            requestedIsoCode: "de",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));

        var application = new ApplicationDescriptor
        {
            ApplicationId = "app",
            DisplayName = new LocalizedText { ["de"] = "App" },
            Description = new LocalizedText { ["de"] = "App" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            Forms =
            [
                new FormDescriptor
                {
                    FormId = FormId.From("current"),
                    DisplayName = new LocalizedText { ["de"] = "Teil 1" },
                    Description = new LocalizedText { ["de"] = "Teil 1" },
                    LatestContractVersion = "1",
                    AvailableContractVersions = ["1"]
                }
            ]
        };

        var context = new FormVoiceCommandContext(
            SessionIdentity: new FormVoiceCommandSessionIdentity(
                IsoCode: "de",
                ApplicationId: "app",
                FormId: "current",
                WakeWordActivationForms: ["okay"]),
            Selection: new FormVoiceCommandSelectionContext(
                VoiceSettings: new VoiceDialogSettings(),
                Application: application,
                CompletedFormIds: ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase),
                Form: application.Forms[0],
                Contract: contract),
            Values: new FormVoiceCommandValueSnapshot(
                ValuesByVariableId: ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
                SharedAdoptions: []));

        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        var promptService = new Mock<IFormVoiceCommandPromptService>(MockBehavior.Strict);
        var autoHelp = new FormVoiceCommandAutoHelpTracker(
            context.VoiceSettings,
            promptService.Object,
            stateTracker,
            interpreter,
            context.IsoCode);

        var field = (await interpreter.GetFocusedFieldAsync());

        var result = await handler.HandleAsync(
            context,
            interpreter,
            field,
            session,
            stateTracker,
            autoHelp,
            CancellationToken.None);

        Assert.NotNull(result.DialogResult);
        Assert.Equal(VoiceCommandNavigationKind.BackToFormSelection, result.DialogResult.Navigation);

        router.Verify(r => r.TryHandleAsync(
                CommandId.From(GlobalCommandIds.SwitchForm),
            It.IsAny<FormVoiceCommandGlobalCommandContext>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenTextStartsWithOkayAndContainsCommandWord_ExecutesCommand()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");

        telemetry
            .Setup(t => t.TrackStateEntry(session, It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackFieldFocus(session, "name"));
        telemetry
            .Setup(t => t.TrackStatusSnapshot(session, It.IsAny<EvaluationFormStatusSnapshot>()));
        telemetry
            .Setup(t => t.TrackFieldAndGroupStatus(session, It.IsAny<EvaluationDetailedFormStatus>()));
        telemetry
            .Setup(t => t.TrackGlobalCommand(session, GlobalCommandIds.Next));
        telemetry
            .Setup(t => t.TrackAmbiguityResolved(
                session,
                It.IsAny<string>(),
                It.IsAny<string>(),
                CommandSignalClass.Clear,
                It.IsAny<string>(),
                "name",
                GlobalCommandIds.Next));

        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService.ForwardConfirmationCaptureToValueCapture();
        speechService
            .Setup(s => s.CaptureTranscriptAsync(
                It.IsAny<string>(),
                "de",
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, string, FormVoiceCommandCaptureSettings, Action, SpeechCaptureTelemetryContext?, RecognitionProfile, SpeechKeyterms?, CancellationToken>((_, _, _, onStartListening, _, _, _, _) =>
            {
                onStartListening();
                return Task.FromResult<SpeechCaptureResult>(new SpeechCaptureResult("okay weiter", null));
            });

        var submissionService = new Mock<IFormVoiceCommandSubmissionService>(MockBehavior.Strict);

        var expectedResult = VoiceCommandDialogResult.BackToFormSelection(
            "app",
            "current",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));

        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        router
            .Setup(r => r.TryHandleAsync(
                It.Is<CommandId>(c => c.Value == "submit"),
                It.IsAny<FormVoiceCommandGlobalCommandContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(VoiceCommandDialogResult.Submitted(
                "app",
                "current",
                ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase)));
        router
            .Setup(r => r.TryHandleAsync(
                CommandId.From(GlobalCommandIds.Next),
                It.IsAny<FormVoiceCommandGlobalCommandContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var handler = CreateFieldInteractionHandler(
            telemetry.Object,
            speechService.Object,
            submissionService.Object,
            router.Object);

        var contract = CreateMinimalContract();
        var interpreter = new FormInterpreterRuntimeOwner(
            contract,
            requestedIsoCode: "de",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));

        var application = new ApplicationDescriptor
        {
            ApplicationId = "app",
            DisplayName = new LocalizedText { ["de"] = "App" },
            Description = new LocalizedText { ["de"] = "App" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            Forms =
            [
                new FormDescriptor
                {
                    FormId = FormId.From("current"),
                    DisplayName = new LocalizedText { ["de"] = "Teil 1" },
                    Description = new LocalizedText { ["de"] = "Teil 1" },
                    LatestContractVersion = "1",
                    AvailableContractVersions = ["1"]
                }
            ]
        };

        var context = new FormVoiceCommandContext(
            SessionIdentity: new FormVoiceCommandSessionIdentity(
                IsoCode: "de",
                ApplicationId: "app",
                FormId: "current",
                WakeWordActivationForms: ["okay"]),
            Selection: new FormVoiceCommandSelectionContext(
                VoiceSettings: new VoiceDialogSettings(),
                Application: application,
                CompletedFormIds: ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase),
                Form: application.Forms[0],
                Contract: contract),
            Values: new FormVoiceCommandValueSnapshot(
                ValuesByVariableId: ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
                SharedAdoptions: []));

        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        var promptService = new Mock<IFormVoiceCommandPromptService>(MockBehavior.Strict);
        var autoHelp = new FormVoiceCommandAutoHelpTracker(
            context.VoiceSettings,
            promptService.Object,
            stateTracker,
            interpreter,
            context.IsoCode);

        var field = (await interpreter.GetFocusedFieldAsync());

        var result = await handler.HandleAsync(
            context,
            interpreter,
            field,
            session,
            stateTracker,
            autoHelp,
            CancellationToken.None);

        Assert.NotNull(result.DialogResult);
        Assert.Equal(VoiceCommandNavigationKind.BackToFormSelection, result.DialogResult.Navigation);

        router.Verify(r => r.TryHandleAsync(
                CommandId.From(GlobalCommandIds.Next),
            It.IsAny<FormVoiceCommandGlobalCommandContext>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenTextContainsCommandWordWithoutOkay_UsesValueCapture()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");
        telemetry.Setup(t => t.TrackGlobalCommand(session, "submit"));

        telemetry
            .Setup(t => t.TrackStateEntry(session, It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackFieldFocus(session, It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackStatusSnapshot(session, It.IsAny<EvaluationFormStatusSnapshot>()));
        telemetry
            .Setup(t => t.TrackFieldAndGroupStatus(session, It.IsAny<EvaluationDetailedFormStatus>()));
        telemetry
            .Setup(t => t.TrackFieldAttempt(session, "name", InputModes.Voice, true, It.IsAny<int?>()));
        // Das Wort „weiter“ bleibt hier ohne Aktivierungswort Teil des Feldwerts und wird als no_command erfasst.
        telemetry
            .Setup(t => t.TrackAmbiguityResolved(
                session,
                "input_vs_command",
                "context_default",
                "no_command",
                "accepted",
                "name",
                It.IsAny<string?>()));

        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService.ForwardConfirmationCaptureToValueCapture();
        // Die simulierten Antworten liefern zuerst den Feldwert und bestätigen anschließend dessen Übernahme.
        var transcripts = new Queue<string?>([
            "ich gehe weiter nach hause",
            "übernehmen"
        ]);
        speechService
            .Setup(s => s.CaptureTranscriptAsync(
                It.IsAny<string>(),
                "de",
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, string, FormVoiceCommandCaptureSettings, Action, SpeechCaptureTelemetryContext?, RecognitionProfile, SpeechKeyterms?, CancellationToken>((_, _, _, onStartListening, _, _, _, _) =>
            {
                onStartListening();
                return Task.FromResult<SpeechCaptureResult>(new SpeechCaptureResult((transcripts.Count > 0 ? transcripts.Dequeue() : "absenden"), null));
            });

        var submissionService = new Mock<IFormVoiceCommandSubmissionService>(MockBehavior.Strict);
        submissionService
            .Setup(s => s.TrySubmitAsync(
                It.IsAny<IFormDialogRuntime>(),
                It.IsAny<FormContract>(),
                session,
                "de",
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<IFormVoiceCommandDialogStateTracker>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FormSubmissionResult.BlockedAwaitingCorrection);

        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        router
            .Setup(r => r.TryHandleAsync(
                It.Is<CommandId>(c => c.Value == "submit"),
                It.IsAny<FormVoiceCommandGlobalCommandContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(VoiceCommandDialogResult.Submitted(
                "app",
                "current",
                ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase)));

        var handler = CreateFieldInteractionHandler(
            telemetry.Object,
            speechService.Object,
            submissionService.Object,
            router.Object);

        var contract = CreateMinimalContract();
        var interpreter = new FormInterpreterRuntimeOwner(
            contract,
            requestedIsoCode: "de",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));

        var application = new ApplicationDescriptor
        {
            ApplicationId = "app",
            DisplayName = new LocalizedText { ["de"] = "App" },
            Description = new LocalizedText { ["de"] = "App" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            Forms =
            [
                new FormDescriptor
                {
                    FormId = FormId.From("current"),
                    DisplayName = new LocalizedText { ["de"] = "Teil 1" },
                    Description = new LocalizedText { ["de"] = "Teil 1" },
                    LatestContractVersion = "1",
                    AvailableContractVersions = ["1"]
                }
            ]
        };

        var context = new FormVoiceCommandContext(
            SessionIdentity: new FormVoiceCommandSessionIdentity(
                IsoCode: "de",
                ApplicationId: "app",
                FormId: "current",
                WakeWordActivationForms: ["okay"]),
            Selection: new FormVoiceCommandSelectionContext(
                VoiceSettings: new VoiceDialogSettings(),
                Application: application,
                CompletedFormIds: ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase),
                Form: application.Forms[0],
                Contract: contract),
            Values: new FormVoiceCommandValueSnapshot(
                ValuesByVariableId: ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
                SharedAdoptions: []));

        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        var promptService = new Mock<IFormVoiceCommandPromptService>(MockBehavior.Strict);
        var autoHelp = new FormVoiceCommandAutoHelpTracker(
            context.VoiceSettings,
            promptService.Object,
            stateTracker,
            interpreter,
            context.IsoCode);

        var field = (await interpreter.GetFocusedFieldAsync());

        var result = await handler.HandleAsync(
            context,
            interpreter,
            field,
            session,
            stateTracker,
            autoHelp,
            CancellationToken.None);

        Assert.Equal(EvaluationOutcome.Submitted, result.DialogResult?.Outcome);
        Assert.Equal("ich gehe weiter nach hause", (await interpreter.GetValuesSnapshotAsync())["name"]);

        router.Verify(r => r.TryHandleAsync(
                It.Is<CommandId>(c => c.Value != "submit"),
            It.IsAny<FormVoiceCommandGlobalCommandContext>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenSpellingInputMode_UsesCanonicalSpellingInConfirmation()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");
        telemetry.Setup(t => t.TrackGlobalCommand(session, "submit"));

        telemetry
            .Setup(t => t.TrackStateEntry(session, It.IsAny<string>()));
        telemetry
            .Setup(t => t.TrackFieldFocus(session, "name"));
        telemetry
            .Setup(t => t.TrackStatusSnapshot(session, It.IsAny<EvaluationFormStatusSnapshot>()));
        telemetry
            .Setup(t => t.TrackFieldAndGroupStatus(session, It.IsAny<EvaluationDetailedFormStatus>()));
        telemetry
            .Setup(t => t.TrackFieldAttempt(session, "name", InputModes.Spelling, true, It.IsAny<int?>()));
        telemetry
            .Setup(t => t.TrackAmbiguityResolved(
                session,
                It.IsAny<string>(),
                It.IsAny<string>(),
                CommandSignalClass.NoCommand,
                It.IsAny<string>(),
                "name",
                It.IsAny<string?>()));

        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService.ForwardConfirmationCaptureToValueCapture();
        speechService.ForwardSingleAttemptCaptureToValueCapture();
        // Auf das Buchstabieren folgen zwei leere Antworten: Die erste führt zur Rückfrage, die zweite beendet das Sammeln.
        // Danach bestätigt „übernehmen“ den Wert.
        var transcripts = new Queue<string?>([
            "a wie anton b wie berta drei",
            null,
            null,
            "übernehmen"
        ]);
        speechService
            .Setup(s => s.CaptureTranscriptAsync(
                It.IsAny<string>(),
                "de",
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, string, FormVoiceCommandCaptureSettings, Action, SpeechCaptureTelemetryContext?, RecognitionProfile, SpeechKeyterms?, CancellationToken>((_, _, _, onStartListening, _, _, _, _) =>
            {
                onStartListening();
                return Task.FromResult<SpeechCaptureResult>(new SpeechCaptureResult((transcripts.Count > 0 ? transcripts.Dequeue() : "absenden"), null));
            });
        // Der Mock erlaubt die Rückfrage nach der ersten leeren Antwort.
        speechService
            .Setup(s => s.SpeakSafeAsync(
                It.IsAny<string>(),
                "de",
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var submissionService = new Mock<IFormVoiceCommandSubmissionService>(MockBehavior.Strict);
        submissionService
            .Setup(s => s.TrySubmitAsync(
                It.IsAny<IFormDialogRuntime>(),
                It.IsAny<FormContract>(),
                session,
                "de",
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<IFormVoiceCommandDialogStateTracker>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FormSubmissionResult.BlockedAwaitingCorrection);

        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        router
            .Setup(r => r.TryHandleAsync(
                It.Is<CommandId>(c => c.Value == "submit"),
                It.IsAny<FormVoiceCommandGlobalCommandContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(VoiceCommandDialogResult.Submitted(
                "app",
                "current",
                ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase)));

        var handler = CreateFieldInteractionHandler(
            telemetry.Object,
            speechService.Object,
            submissionService.Object,
            router.Object);

        var contract = CreateMinimalContract([InputModes.Spelling]);
        var interpreter = new FormInterpreterRuntimeOwner(
            contract,
            requestedIsoCode: "de",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));

        var application = new ApplicationDescriptor
        {
            ApplicationId = "app",
            DisplayName = new LocalizedText { ["de"] = "App" },
            Description = new LocalizedText { ["de"] = "App" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            Forms =
            [
                new FormDescriptor
                {
                    FormId = FormId.From("current"),
                    DisplayName = new LocalizedText { ["de"] = "Teil 1" },
                    Description = new LocalizedText { ["de"] = "Teil 1" },
                    LatestContractVersion = "1",
                    AvailableContractVersions = ["1"]
                }
            ]
        };

        var context = new FormVoiceCommandContext(
            SessionIdentity: new FormVoiceCommandSessionIdentity(
                IsoCode: "de",
                ApplicationId: "app",
                FormId: "current",
                WakeWordActivationForms: ["okay"]),
            Selection: new FormVoiceCommandSelectionContext(
                VoiceSettings: new VoiceDialogSettings(),
                Application: application,
                CompletedFormIds: ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase),
                Form: application.Forms[0],
                Contract: contract),
            Values: new FormVoiceCommandValueSnapshot(
                ValuesByVariableId: ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
                SharedAdoptions: []));

        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        var promptService = new Mock<IFormVoiceCommandPromptService>(MockBehavior.Strict);
        var autoHelp = new FormVoiceCommandAutoHelpTracker(
            context.VoiceSettings,
            promptService.Object,
            stateTracker,
            interpreter,
            context.IsoCode);

        var field = (await interpreter.GetFocusedFieldAsync());

        var result = await handler.HandleAsync(
            context,
            interpreter,
            field,
            session,
            stateTracker,
            autoHelp,
            CancellationToken.None);

        Assert.Equal(EvaluationOutcome.Submitted, result.DialogResult?.Outcome);

        telemetry.Verify(t => t.TrackFieldAttempt(session, "name", InputModes.Spelling, true, It.IsAny<int?>()), Times.Once);
    }

    // Im wörtlichen Modus wird „weiter“ als Feldwert übernommen und nicht als Navigationsbefehl ausgeführt.
    // Nach der Wertbestätigung liefert der Mock „absenden“; der Router gibt das Abschlussergebnis zurück.
    [Fact]
    public async Task HandleAsync_WhenLiteralModeAndCommandDetected_CapturesCommandWordAsValue()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");
        telemetry.Setup(t => t.TrackGlobalCommand(session, "submit"));

        telemetry.Setup(t => t.TrackStateEntry(session, It.IsAny<string>()));
        telemetry.Setup(t => t.TrackFieldFocus(session, It.IsAny<string>()));
        telemetry.Setup(t => t.TrackStatusSnapshot(session, It.IsAny<EvaluationFormStatusSnapshot>()));
        telemetry.Setup(t => t.TrackFieldAndGroupStatus(session, It.IsAny<EvaluationDetailedFormStatus>()));
        telemetry.Setup(t => t.TrackFieldAttempt(session, "name", InputModes.Literal, true, It.IsAny<int?>()));

        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService.ForwardConfirmationCaptureToValueCapture();
        var transcripts = new Queue<string?>([
            "weiter",
            "übernehmen"
        ]);
        speechService
            .Setup(s => s.CaptureTranscriptAsync(
                It.IsAny<string>(),
                "de",
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, string, FormVoiceCommandCaptureSettings, Action, SpeechCaptureTelemetryContext?, RecognitionProfile, SpeechKeyterms?, CancellationToken>((_, _, _, onStartListening, _, _, _, _) =>
            {
                onStartListening();
                return Task.FromResult<SpeechCaptureResult>(new SpeechCaptureResult((transcripts.Count > 0 ? transcripts.Dequeue() : "absenden"), null));
            });

        var submissionService = new Mock<IFormVoiceCommandSubmissionService>(MockBehavior.Strict);
        submissionService
            .Setup(s => s.TrySubmitAsync(
                It.IsAny<IFormDialogRuntime>(),
                It.IsAny<FormContract>(),
                session,
                "de",
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<IFormVoiceCommandDialogStateTracker>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FormSubmissionResult.BlockedAwaitingCorrection);
        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        router
            .Setup(r => r.TryHandleAsync(
                It.Is<CommandId>(c => c.Value == "submit"),
                It.IsAny<FormVoiceCommandGlobalCommandContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(VoiceCommandDialogResult.Submitted(
                "app",
                "current",
                ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase)));

        var handler = CreateFieldInteractionHandler(
            telemetry.Object,
            speechService.Object,
            submissionService.Object,
            router.Object);

        var contract = CreateMinimalContract([InputModes.Literal]);
        var interpreter = new FormInterpreterRuntimeOwner(
            contract,
            requestedIsoCode: "de",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));

        var context = CreateContext(contract);
        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        var promptService = new Mock<IFormVoiceCommandPromptService>(MockBehavior.Strict);
        var autoHelp = new FormVoiceCommandAutoHelpTracker(
            context.VoiceSettings,
            promptService.Object,
            stateTracker,
            interpreter,
            context.IsoCode);

        var field = (await interpreter.GetFocusedFieldAsync());

        var result = await handler.HandleAsync(
            context,
            interpreter,
            field,
            session,
            stateTracker,
            autoHelp,
            CancellationToken.None);

        Assert.Equal(EvaluationOutcome.Submitted, result.DialogResult?.Outcome);
        Assert.Equal("weiter", (await interpreter.GetValuesSnapshotAsync())["name"]);
        router.Verify(r => r.TryHandleAsync(
                It.Is<CommandId>(c => c.Value != "submit"),
            It.IsAny<FormVoiceCommandGlobalCommandContext>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenKeyboardModeParkExecutesVoiceCommand_KeepsFocusAndContinues()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(s => s.CaptureConfirmationTranscriptAsync(
                It.IsAny<string>(),
                "de",
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, string, FormVoiceCommandCaptureSettings, Action, SpeechCaptureTelemetryContext?, SpeechKeyterms?, CancellationToken>((_, _, _, onStartListening, _, _, _) =>
            {
                onStartListening();
                return Task.FromResult<SpeechCaptureResult>(new SpeechCaptureResult("befehl spracheingabe", null));
            });
        speechService
            .Setup(s => s.SpeakSafeAsync(
                It.IsAny<string>(),
                "de",
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var router = new FormVoiceCommandGlobalCommandRouter(
            [new FormVoiceCommandInputModeHandler(telemetry.Object, speechService.Object)]);
        var submissionService = new Mock<IFormVoiceCommandSubmissionService>(MockBehavior.Strict);
        var handler = CreateFieldInteractionHandler(
            telemetry.Object,
            speechService.Object,
            submissionService.Object,
            router);

        var contract = CreateMinimalContract();
        var interpreter = new FormInterpreterRuntimeOwner(
            contract,
            requestedIsoCode: "de",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));
        Assert.True(await interpreter.TrySetFocusedFieldInputModeAsync(InputModes.Keyboard));

        var context = CreateContext(contract);
        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        var autoHelp = new FormVoiceCommandAutoHelpTracker(
            context.VoiceSettings,
            Mock.Of<IFormVoiceCommandPromptService>(),
            stateTracker,
            interpreter,
            context.IsoCode);
        var field = await interpreter.GetFocusedFieldAsync();

        var result = await handler.HandleAsync(
            context,
            interpreter,
            field,
            session,
            stateTracker,
            autoHelp,
            CancellationToken.None);

        Assert.True(result.ShouldContinue);
        Assert.NotNull(interpreter.MutableState.FocusedFieldId);
        Assert.Equal(InputMode.Voice, (await interpreter.GetFocusedFieldAsync()).InputMode);
    }

    [Fact]
    public async Task HandleAsync_WhenKeyboardModeActive_BlocksVoiceValueCapture()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");

        telemetry.Setup(t => t.TrackStateEntry(session, It.IsAny<string>()));
        telemetry.Setup(t => t.TrackFieldFocus(session, It.IsAny<string>()));
        telemetry.Setup(t => t.TrackStatusSnapshot(session, It.IsAny<EvaluationFormStatusSnapshot>()));
        telemetry.Setup(t => t.TrackFieldAndGroupStatus(session, It.IsAny<EvaluationDetailedFormStatus>()));
        telemetry.Setup(t => t.TrackFieldAttempt(session, "name", InputModes.Keyboard, false, It.IsAny<int?>()));
        telemetry.Setup(t => t.TrackConstraintViolation(session, "name", "keyboard_mode_active", "warning", TelemetryErrorTriggers.UserInput));
        telemetry.Setup(t => t.TrackRepairAttempt(session, "name", "keyboard_mode_active"));

        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService.ForwardConfirmationCaptureToValueCapture();
        speechService
            .Setup(s => s.CaptureTranscriptAsync(
                It.IsAny<string>(),
                "de",
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, string, FormVoiceCommandCaptureSettings, Action, SpeechCaptureTelemetryContext?, RecognitionProfile, SpeechKeyterms?, CancellationToken>((_, _, _, onStartListening, _, _, _, _) =>
            {
                onStartListening();
                return Task.FromResult<SpeechCaptureResult>(new SpeechCaptureResult("Max", null));
            });
        speechService
            .Setup(s => s.SpeakSafeAsync(
                FormInputFeedbackPromptBuilder.GetVoiceInputForbiddenPrompt("de", null),
                "de",
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var submissionService = new Mock<IFormVoiceCommandSubmissionService>(MockBehavior.Strict);
        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        router
            .Setup(r => r.TryHandleAsync(
                It.Is<CommandId>(c => c.Value == "submit"),
                It.IsAny<FormVoiceCommandGlobalCommandContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(VoiceCommandDialogResult.Submitted(
                "app",
                "current",
                ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase)));

        var handler = CreateFieldInteractionHandler(
            telemetry.Object,
            speechService.Object,
            submissionService.Object,
            router.Object);

        var contract = CreateMinimalContract();
        var interpreter = new FormInterpreterRuntimeOwner(
            contract,
            requestedIsoCode: "de",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));
        Assert.True(await interpreter.TrySetFocusedFieldInputModeAsync(InputModes.Keyboard));

        var context = CreateContext(contract);
        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        var promptService = new Mock<IFormVoiceCommandPromptService>(MockBehavior.Strict);
        var autoHelp = new FormVoiceCommandAutoHelpTracker(
            context.VoiceSettings,
            promptService.Object,
            stateTracker,
            interpreter,
            context.IsoCode);

        var field = (await interpreter.GetFocusedFieldAsync());

        var result = await handler.HandleAsync(
            context,
            interpreter,
            field,
            session,
            stateTracker,
            autoHelp,
            CancellationToken.None);

        Assert.Null(result.DialogResult);
        Assert.False((await interpreter.GetValuesSnapshotAsync()).ContainsKey("name"));
    }

    [Fact]
    public async Task HandleAsync_WhenVoiceInputIsForbiddenByPolicy_PromptsSilentFallback()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");

        telemetry.Setup(t => t.TrackStateEntry(session, It.IsAny<string>()));
        telemetry.Setup(t => t.TrackFieldFocus(session, It.IsAny<string>()));
        telemetry.Setup(t => t.TrackStatusSnapshot(session, It.IsAny<EvaluationFormStatusSnapshot>()));
        telemetry.Setup(t => t.TrackFieldAndGroupStatus(session, It.IsAny<EvaluationDetailedFormStatus>()));
        telemetry.Setup(t => t.TrackFieldAttempt(session, "name", InputModes.Voice, false, It.IsAny<int?>()));
        telemetry.Setup(t => t.TrackConstraintViolation(session, "name", "voice_input_forbidden", "warning", TelemetryErrorTriggers.UserInput));
        telemetry.Setup(t => t.TrackRepairAttempt(session, "name", "voice_input_forbidden"));

        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService.ForwardConfirmationCaptureToValueCapture();
        speechService
            .Setup(s => s.CaptureTranscriptAsync(
                It.IsAny<string>(),
                "de",
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, string, FormVoiceCommandCaptureSettings, Action, SpeechCaptureTelemetryContext?, RecognitionProfile, SpeechKeyterms?, CancellationToken>((_, _, _, onStartListening, _, _, _, _) =>
            {
                onStartListening();
                return Task.FromResult<SpeechCaptureResult>(new SpeechCaptureResult("Max", null));
            });
        speechService
            .Setup(s => s.SpeakSafeAsync(
                FormInputFeedbackPromptBuilder.GetSilentInputOfferPrompt("de", "die Tastatur"),
                "de",
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        speechService
            .Setup(s => s.SpeakSafeAsync(
                FormInputFeedbackPromptBuilder.GetVoiceInputForbiddenPrompt("de", "die Tastatur"),
                "de",
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var submissionService = new Mock<IFormVoiceCommandSubmissionService>(MockBehavior.Strict);
        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        router
            .Setup(r => r.TryHandleAsync(
                It.Is<CommandId>(c => c.Value == "submit"),
                It.IsAny<FormVoiceCommandGlobalCommandContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(VoiceCommandDialogResult.Submitted(
                "app",
                "current",
                ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase)));

        var handler = CreateFieldInteractionHandler(
            telemetry.Object,
            speechService.Object,
            submissionService.Object,
            router.Object);

        var contract = CreateMinimalContract(
            privacyClass: PrivacyClass.Secret,
            sensitiveDataPolicy: new SensitiveDataPolicy
            {
                SilentInputFallback = SensitiveDataPolicyDefaults.SilentInputFallbackKeyboard
            });
        var interpreter = new FormInterpreterRuntimeOwner(
            contract,
            requestedIsoCode: "de",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));

        var context = CreateContext(contract);
        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        var promptService = new Mock<IFormVoiceCommandPromptService>(MockBehavior.Strict);
        var autoHelp = new FormVoiceCommandAutoHelpTracker(
            context.VoiceSettings,
            promptService.Object,
            stateTracker,
            interpreter,
            context.IsoCode);

        var field = (await interpreter.GetFocusedFieldAsync());

        var result = await handler.HandleAsync(
            context,
            interpreter,
            field,
            session,
            stateTracker,
            autoHelp,
            CancellationToken.None);

        Assert.Null(result.DialogResult);
        Assert.False((await interpreter.GetValuesSnapshotAsync()).ContainsKey("name"));
    }

    [Fact]
    public async Task HandleAsync_WhenReadbackOptInRequired_RequestsConsentAndReadsValue()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");
        telemetry.Setup(t => t.TrackGlobalCommand(session, "submit"));

        telemetry.Setup(t => t.TrackStateEntry(session, It.IsAny<string>()));
        telemetry.Setup(t => t.TrackFieldFocus(session, It.IsAny<string>()));
        telemetry.Setup(t => t.TrackStatusSnapshot(session, It.IsAny<EvaluationFormStatusSnapshot>()));
        telemetry.Setup(t => t.TrackFieldAndGroupStatus(session, It.IsAny<EvaluationDetailedFormStatus>()));
        telemetry.Setup(t => t.TrackFieldAttempt(session, "name", InputModes.Voice, true, It.IsAny<int?>()));
        telemetry.Setup(t => t.TrackAmbiguityResolved(
            session,
            It.IsAny<string>(),
            It.IsAny<string>(),
            CommandSignalClass.NoCommand,
            It.IsAny<string>(),
            "name",
            It.IsAny<string?>()));

        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService.ForwardConfirmationCaptureToValueCapture();
        var transcripts = new Queue<string?>([
            "Geheimwert",
            "übernehmen"
        ]);
        speechService
            .Setup(s => s.CaptureTranscriptAsync(
                It.IsAny<string>(),
                "de",
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, string, FormVoiceCommandCaptureSettings, Action, SpeechCaptureTelemetryContext?, RecognitionProfile, SpeechKeyterms?, CancellationToken>((_, _, _, onStartListening, _, _, _, _) =>
            {
                onStartListening();
                return Task.FromResult<SpeechCaptureResult>(new SpeechCaptureResult((transcripts.Count > 0 ? transcripts.Dequeue() : "absenden"), null));
            });
        speechService
            .Setup(s => s.SpeakSafeAsync(
                FormInputFeedbackPromptBuilder.GetSilentInputOfferPrompt("de", null),
                "de",
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        speechService
            .Setup(s => s.ConfirmYesNoAsync(
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                FormInputFeedbackPromptBuilder.GetReadbackOptInPrompt("de", "Name", PolitenessLevel.Formal),
                "de",
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var submissionService = new Mock<IFormVoiceCommandSubmissionService>(MockBehavior.Strict);
        submissionService
            .Setup(s => s.TrySubmitAsync(
                It.IsAny<IFormDialogRuntime>(),
                It.IsAny<FormContract>(),
                session,
                "de",
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<IFormVoiceCommandDialogStateTracker>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FormSubmissionResult.BlockedAwaitingCorrection);

        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        router
            .Setup(r => r.TryHandleAsync(
                It.Is<CommandId>(c => c.Value == "submit"),
                It.IsAny<FormVoiceCommandGlobalCommandContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(VoiceCommandDialogResult.Submitted(
                "app",
                "current",
                ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase)));

        var handler = CreateFieldInteractionHandler(
            telemetry.Object,
            speechService.Object,
            submissionService.Object,
            router.Object);

        var contract = CreateMinimalContract(privacyClass: PrivacyClass.Sensitive);
        var interpreter = new FormInterpreterRuntimeOwner(
            contract,
            requestedIsoCode: "de",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));

        var context = CreateContext(contract);
        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        var promptService = new Mock<IFormVoiceCommandPromptService>(MockBehavior.Strict);
        var autoHelp = new FormVoiceCommandAutoHelpTracker(
            context.VoiceSettings,
            promptService.Object,
            stateTracker,
            interpreter,
            context.IsoCode);

        var field = (await interpreter.GetFocusedFieldAsync());

        var result = await handler.HandleAsync(
            context,
            interpreter,
            field,
            session,
            stateTracker,
            autoHelp,
            CancellationToken.None);

        Assert.Equal(EvaluationOutcome.Submitted, result.DialogResult?.Outcome);
        Assert.Equal("Geheimwert", (await interpreter.GetValuesSnapshotAsync())["name"]);
    }

    [Fact]
    public async Task HandleAsync_WhenConfirmationActionIsEdit_RequestsCorrectionAndKeepsFieldFocus()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");

        telemetry.Setup(t => t.TrackStateEntry(session, It.IsAny<string>()));
        telemetry.Setup(t => t.TrackFieldFocus(session, It.IsAny<string>()));
        telemetry.Setup(t => t.TrackStatusSnapshot(session, It.IsAny<EvaluationFormStatusSnapshot>()));
        telemetry.Setup(t => t.TrackFieldAndGroupStatus(session, It.IsAny<EvaluationDetailedFormStatus>()));
        telemetry.Setup(t => t.TrackFieldAttempt(session, "name", InputModes.Voice, true, It.IsAny<int?>()));
        telemetry.Setup(t => t.TrackRepairAttempt(session, "name", "manual_correction"));
        telemetry.Setup(t => t.TrackAmbiguityResolved(
            session,
            It.IsAny<string>(),
            It.IsAny<string>(),
            CommandSignalClass.NoCommand,
            It.IsAny<string>(),
            "name",
            It.IsAny<string?>()));

        var transcripts = new Queue<string?>([
            "Max",
            "ändern"
        ]);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService.ForwardConfirmationCaptureToValueCapture();
        speechService
            .Setup(s => s.CaptureTranscriptAsync(
                It.IsAny<string>(),
                "de",
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, string, FormVoiceCommandCaptureSettings, Action, SpeechCaptureTelemetryContext?, RecognitionProfile, SpeechKeyterms?, CancellationToken>((_, _, _, onStartListening, _, _, _, _) =>
            {
                onStartListening();
                return Task.FromResult<SpeechCaptureResult>(new SpeechCaptureResult((transcripts.Count > 0 ? transcripts.Dequeue() : "absenden"), null));
            });
        speechService
            .Setup(s => s.SpeakSafeAsync(
                FormInputFeedbackPromptBuilder.GetCorrectionRequestedPrompt("de"),
                "de",
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var submissionService = new Mock<IFormVoiceCommandSubmissionService>(MockBehavior.Strict);
        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        router
            .Setup(r => r.TryHandleAsync(
                It.Is<CommandId>(c => c.Value == "submit"),
                It.IsAny<FormVoiceCommandGlobalCommandContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(VoiceCommandDialogResult.Submitted(
                "app",
                "current",
                ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase)));
        var handler = CreateFieldInteractionHandler(
            telemetry.Object,
            speechService.Object,
            submissionService.Object,
            router.Object);

        var contract = CreateMinimalContract();
        var interpreter = new FormInterpreterRuntimeOwner(
            contract,
            requestedIsoCode: "de",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));
        var context = CreateContext(contract);
        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        var promptService = new Mock<IFormVoiceCommandPromptService>(MockBehavior.Strict);
        var autoHelp = new FormVoiceCommandAutoHelpTracker(
            context.VoiceSettings,
            promptService.Object,
            stateTracker,
            interpreter,
            context.IsoCode);

        var result = await handler.HandleAsync(
            context,
            interpreter,
            (await interpreter.GetFocusedFieldAsync()),
            session,
            stateTracker,
            autoHelp,
            CancellationToken.None);

        Assert.Null(result.DialogResult);
        Assert.Equal("Max", (await interpreter.GetValuesSnapshotAsync())["name"]);
        submissionService.Verify(s => s.TrySubmitAsync(
            It.IsAny<IFormDialogRuntime>(),
            It.IsAny<FormContract>(),
            It.IsAny<EvaluationSession>(),
            It.IsAny<string>(),
            It.IsAny<VoiceDialogSettings>(),
            It.IsAny<IFormVoiceCommandDialogStateTracker>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenDigitsModeUsesCorrectionCommands_AppliesEditedDigits()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");
        telemetry.Setup(t => t.TrackGlobalCommand(session, "submit"));

        telemetry.Setup(t => t.TrackStateEntry(session, It.IsAny<string>()));
        telemetry.Setup(t => t.TrackFieldFocus(session, It.IsAny<string>()));
        telemetry.Setup(t => t.TrackStatusSnapshot(session, It.IsAny<EvaluationFormStatusSnapshot>()));
        telemetry.Setup(t => t.TrackFieldAndGroupStatus(session, It.IsAny<EvaluationDetailedFormStatus>()));
        telemetry.Setup(t => t.TrackFieldAttempt(session, "name", InputModes.Digits, true, It.IsAny<int?>()));
        telemetry.Setup(t => t.TrackRepairAttempt(session, "name", It.IsAny<string>()));

        var transcripts = new Queue<string?>([
            "eins zwei drei",
            "letzte Ziffer löschen",
            "vier",
            "weiter",
            "übernehmen"
        ]);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService.ForwardConfirmationCaptureToValueCapture();
        speechService.ForwardSingleAttemptCaptureToValueCapture();
        speechService
            .Setup(s => s.CaptureTranscriptAsync(
                It.IsAny<string>(),
                "de",
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, string, FormVoiceCommandCaptureSettings, Action, SpeechCaptureTelemetryContext?, RecognitionProfile, SpeechKeyterms?, CancellationToken>((_, _, _, onStartListening, _, _, _, _) =>
            {
                onStartListening();
                return Task.FromResult<SpeechCaptureResult>(new SpeechCaptureResult((transcripts.Count > 0 ? transcripts.Dequeue() : "absenden"), null));
            });
        speechService
            .Setup(s => s.SpeakSafeAsync(It.IsAny<string>(), "de", It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var submissionService = new Mock<IFormVoiceCommandSubmissionService>(MockBehavior.Strict);
        submissionService
            .Setup(s => s.TrySubmitAsync(
                It.IsAny<IFormDialogRuntime>(),
                It.IsAny<FormContract>(),
                session,
                "de",
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<IFormVoiceCommandDialogStateTracker>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FormSubmissionResult.BlockedAwaitingCorrection);

        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        router
            .Setup(r => r.TryHandleAsync(
                It.Is<CommandId>(c => c.Value == "submit"),
                It.IsAny<FormVoiceCommandGlobalCommandContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(VoiceCommandDialogResult.Submitted(
                "app",
                "current",
                ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase)));
        var handler = CreateFieldInteractionHandler(
            telemetry.Object,
            speechService.Object,
            submissionService.Object,
            router.Object);

        var contract = CreateMinimalContract([InputModes.Digits], formatHint: "phone");
        var interpreter = new FormInterpreterRuntimeOwner(
            contract,
            requestedIsoCode: "de",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));
        var context = CreateContext(contract);
        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        var promptService = new Mock<IFormVoiceCommandPromptService>(MockBehavior.Strict);
        var autoHelp = new FormVoiceCommandAutoHelpTracker(
            context.VoiceSettings,
            promptService.Object,
            stateTracker,
            interpreter,
            context.IsoCode);

        var result = await handler.HandleAsync(
            context,
            interpreter,
            (await interpreter.GetFocusedFieldAsync()),
            session,
            stateTracker,
            autoHelp,
            CancellationToken.None);

        Assert.Equal(EvaluationOutcome.Submitted, result.DialogResult?.Outcome);
        Assert.Equal("124", (await interpreter.GetValuesSnapshotAsync())["name"]);
    }

    [Fact]
    public async Task HandleAsync_WhenSensitiveReadbackNotFullyAllowed_UsesMaskedPrompt()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");
        telemetry.Setup(t => t.TrackGlobalCommand(session, "submit"));

        telemetry.Setup(t => t.TrackStateEntry(session, It.IsAny<string>()));
        telemetry.Setup(t => t.TrackFieldFocus(session, It.IsAny<string>()));
        telemetry.Setup(t => t.TrackStatusSnapshot(session, It.IsAny<EvaluationFormStatusSnapshot>()));
        telemetry.Setup(t => t.TrackFieldAndGroupStatus(session, It.IsAny<EvaluationDetailedFormStatus>()));
        telemetry.Setup(t => t.TrackFieldAttempt(session, "name", InputModes.Voice, true, It.IsAny<int?>()));
        telemetry.Setup(t => t.TrackAmbiguityResolved(
            session,
            It.IsAny<string>(),
            It.IsAny<string>(),
            CommandSignalClass.NoCommand,
            It.IsAny<string>(),
            "name",
            It.IsAny<string?>()));

        var transcripts = new Queue<string?>([
            "Geheimwert",
            "übernehmen"
        ]);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService.ForwardConfirmationCaptureToValueCapture();
        speechService
            .Setup(s => s.CaptureTranscriptAsync(
                It.Is<string>(prompt => prompt.Contains("Endet auf", StringComparison.Ordinal) || prompt.Contains("Name", StringComparison.Ordinal) || prompt.Contains("Alle Felder", StringComparison.Ordinal)),
                "de",
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, string, FormVoiceCommandCaptureSettings, Action, SpeechCaptureTelemetryContext?, RecognitionProfile, SpeechKeyterms?, CancellationToken>((_, _, _, onStartListening, _, _, _, _) =>
            {
                onStartListening();
                return Task.FromResult<SpeechCaptureResult>(new SpeechCaptureResult((transcripts.Count > 0 ? transcripts.Dequeue() : "absenden"), null));
            });
        speechService
            .Setup(s => s.SpeakSafeAsync(It.IsAny<string>(), "de", It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        speechService
            .Setup(s => s.ConfirmYesNoAsync(
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<string>(),
                "de",
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var submissionService = new Mock<IFormVoiceCommandSubmissionService>(MockBehavior.Strict);
        submissionService
            .Setup(s => s.TrySubmitAsync(
                It.IsAny<IFormDialogRuntime>(),
                It.IsAny<FormContract>(),
                session,
                "de",
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<IFormVoiceCommandDialogStateTracker>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(FormSubmissionResult.BlockedAwaitingCorrection);

        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        router
            .Setup(r => r.TryHandleAsync(
                It.Is<CommandId>(c => c.Value == "submit"),
                It.IsAny<FormVoiceCommandGlobalCommandContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(VoiceCommandDialogResult.Submitted(
                "app",
                "current",
                ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase)));
        var handler = CreateFieldInteractionHandler(
            telemetry.Object,
            speechService.Object,
            submissionService.Object,
            router.Object);

        var contract = CreateMinimalContract(privacyClass: PrivacyClass.Sensitive);
        var interpreter = new FormInterpreterRuntimeOwner(
            contract,
            requestedIsoCode: "de",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));
        var context = CreateContext(contract);
        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        var promptService = new Mock<IFormVoiceCommandPromptService>(MockBehavior.Strict);
        var autoHelp = new FormVoiceCommandAutoHelpTracker(
            context.VoiceSettings,
            promptService.Object,
            stateTracker,
            interpreter,
            context.IsoCode);

        var result = await handler.HandleAsync(
            context,
            interpreter,
            (await interpreter.GetFocusedFieldAsync()),
            session,
            stateTracker,
            autoHelp,
            CancellationToken.None);

        Assert.Equal(EvaluationOutcome.Submitted, result.DialogResult?.Outcome);
        Assert.Equal("Geheimwert", (await interpreter.GetValuesSnapshotAsync())["name"]);
    }

    // Kennzeichnet die Strukturerklärung, die im reinen Datumsbeispiel fehlt.
    private const string DateStructureHintMarker = "vollständiges Datum";

    private const string DateExamplePhrase = "achter Januar neunzehnhundertachtzig";

    [Fact]
    public async Task HandleAsync_FirstDateFieldInSession_ProactivelyTeachesDateStructureWithExample()
    {
        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");
        var stateTracker = new FormVoiceCommandStateTracker(new Mock<IEvaluationTelemetry>().Object, session);
        var contract = CreateDateContract(("birthDate", IsDate: true));

        var entryPrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 0, session, stateTracker);

        Assert.Contains(DateStructureHintMarker, entryPrompt, StringComparison.Ordinal);
        Assert.Contains(DateExamplePhrase, entryPrompt, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_LaterDateFieldInSameSession_SpeaksOnlyExampleWithoutStructureHint()
    {
        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");
        var stateTracker = new FormVoiceCommandStateTracker(new Mock<IEvaluationTelemetry>().Object, session);
        var contract = CreateDateContract(("birthDate", IsDate: true), ("startDate", IsDate: true));

        var firstEntryPrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 0, session, stateTracker);
        var laterEntryPrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 1, session, stateTracker);

        Assert.Contains(DateStructureHintMarker, firstEntryPrompt, StringComparison.Ordinal);

        Assert.DoesNotContain(DateStructureHintMarker, laterEntryPrompt, StringComparison.Ordinal);
        Assert.Contains(DateExamplePhrase, laterEntryPrompt, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_ReEnteringTheSameDateField_DoesNotRepeatTheStructureHint()
    {
        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");
        var stateTracker = new FormVoiceCommandStateTracker(new Mock<IEvaluationTelemetry>().Object, session);
        var contract = CreateDateContract(("birthDate", IsDate: true));

        var firstEntryPrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 0, session, stateTracker);
        var reEntryPrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 0, session, stateTracker);

        Assert.Contains(DateStructureHintMarker, firstEntryPrompt, StringComparison.Ordinal);

        Assert.DoesNotContain(DateStructureHintMarker, reEntryPrompt, StringComparison.Ordinal);
        Assert.Contains(DateExamplePhrase, reEntryPrompt, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("de")]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("it")]
    public async Task HandleAsync_DateRecognitionFailureReprompt_ReTeachesStructureAndThenClearsTheMarker(string isoCode)
    {
        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");
        var stateTracker = new FormVoiceCommandStateTracker(new Mock<IEvaluationTelemetry>().Object, session);
        var contract = CreateDateContract(("birthDate", IsDate: true));
        var structuralHint = FormDateEntryExamplePromptBuilder.GetDateEntryDecompositionHintPrompt(
            contract,
            isoCode,
            new VoiceDialogSettings().GetPolitenessLevel());

        var firstEntryPrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 0, session, stateTracker, isoCode);
        var normalReEntryPrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 0, session, stateTracker, isoCode);
        stateTracker.ArmStructuralTeachingReteach("birthDate");
        var failureReprompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 0, session, stateTracker, isoCode);
        var afterFailurePrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 0, session, stateTracker, isoCode);

        Assert.False(string.IsNullOrWhiteSpace(structuralHint));
        Assert.Contains(structuralHint!, firstEntryPrompt, StringComparison.Ordinal);
        Assert.DoesNotContain(structuralHint!, normalReEntryPrompt, StringComparison.Ordinal);
        Assert.Contains(structuralHint!, failureReprompt, StringComparison.Ordinal);
        Assert.DoesNotContain(structuralHint!, afterFailurePrompt, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_DateReteachMarkerForDifferentField_DoesNotLiftCurrentFieldAndPreservesMarker()
    {
        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");
        var stateTracker = new FormVoiceCommandStateTracker(new Mock<IEvaluationTelemetry>().Object, session);
        var contract = CreateDateContract(("birthDate", IsDate: true), ("startDate", IsDate: true));

        var firstEntryPrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 0, session, stateTracker);
        stateTracker.ArmStructuralTeachingReteach("birthDate");
        var otherDatePrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 1, session, stateTracker);
        var originalDatePrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 0, session, stateTracker);

        Assert.Contains(DateStructureHintMarker, firstEntryPrompt, StringComparison.Ordinal);
        Assert.DoesNotContain(DateStructureHintMarker, otherDatePrompt, StringComparison.Ordinal);
        Assert.Contains(DateStructureHintMarker, originalDatePrompt, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_NonDateField_NeverTeachesDateStructure_AndDoesNotConsumeTheLatch()
    {
        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");
        var stateTracker = new FormVoiceCommandStateTracker(new Mock<IEvaluationTelemetry>().Object, session);
        var contract = CreateDateContract(("name", IsDate: false), ("birthDate", IsDate: true));

        var nonDateEntryPrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 0, session, stateTracker);
        var dateEntryPrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 1, session, stateTracker);

        Assert.DoesNotContain(DateStructureHintMarker, nonDateEntryPrompt, StringComparison.Ordinal);
        Assert.DoesNotContain(DateExamplePhrase, nonDateEntryPrompt, StringComparison.Ordinal);

        Assert.Contains(DateStructureHintMarker, dateEntryPrompt, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_NonStructuralReteachMarker_ConsumesHarmlesslyWithoutSpendingTheDateLatch()
    {
        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");
        var stateTracker = new FormVoiceCommandStateTracker(new Mock<IEvaluationTelemetry>().Object, session);
        var contract = CreateDateContract(("name", IsDate: false), ("birthDate", IsDate: true));

        stateTracker.ArmStructuralTeachingReteach("name");
        var nonDateEntryPrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 0, session, stateTracker);
        var dateEntryPrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 1, session, stateTracker);

        Assert.DoesNotContain(DateStructureHintMarker, nonDateEntryPrompt, StringComparison.Ordinal);
        Assert.DoesNotContain(DateExamplePhrase, nonDateEntryPrompt, StringComparison.Ordinal);
        Assert.False(stateTracker.ConsumeStructuralTeachingReteach("name"));
        Assert.Contains(DateStructureHintMarker, dateEntryPrompt, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_NewSession_ReArmsDateStructureTeaching()
    {
        // Jede Sitzung erhält einen eigenen Zustandstracker für die einmaligen Erläuterungen.
        var contract = CreateDateContract(("birthDate", IsDate: true));

        var firstSession = new EvaluationSession("session-1", EvaluationChannel.Voice, "test-app", "form-1");
        var firstTracker = new FormVoiceCommandStateTracker(new Mock<IEvaluationTelemetry>().Object, firstSession);
        var firstSessionPrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 0, firstSession, firstTracker);

        var secondSession = new EvaluationSession("session-2", EvaluationChannel.Voice, "test-app", "form-1");
        var secondTracker = new FormVoiceCommandStateTracker(new Mock<IEvaluationTelemetry>().Object, secondSession);
        var secondSessionPrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 0, secondSession, secondTracker);

        Assert.Contains(DateStructureHintMarker, firstSessionPrompt, StringComparison.Ordinal);
        Assert.Contains(DateStructureHintMarker, secondSessionPrompt, StringComparison.Ordinal);
    }

    // Kennzeichnet die Einführung zur Mehrfachauswahl, die im Hinweis zur Anzahl fehlt.
    private const string SetStructureHintMarker = "eine oder mehrere Optionen";

    private const string SetSelectionCountMarker = "2 bis 4 Optionen";

    // Kennzeichnet das Angebot, die aktuelle Auswahl zu übernehmen.
    private const string SetExitCueMarker = "Übernehmen";

    [Fact]
    public async Task HandleAsync_FirstMultiSelectFieldInSession_TeachesAccumulationStructureAtEntry()
    {
        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");
        var stateTracker = new FormVoiceCommandStateTracker(new Mock<IEvaluationTelemetry>().Object, session);
        var contract = CreateSetContract(("colors", IsSet: true));

        var entryPrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 0, session, stateTracker);

        Assert.Contains(SetStructureHintMarker, entryPrompt, StringComparison.Ordinal);
        Assert.Contains(SetSelectionCountMarker, entryPrompt, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_LaterMultiSelectFieldInSameSession_SpeaksOnlySelectionCountWithoutStructureTeaching()
    {
        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");
        var stateTracker = new FormVoiceCommandStateTracker(new Mock<IEvaluationTelemetry>().Object, session);
        var contract = CreateSetContract(("colors", IsSet: true), ("toppings", IsSet: true));

        var firstEntryPrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 0, session, stateTracker);
        var laterEntryPrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 1, session, stateTracker);

        Assert.Contains(SetStructureHintMarker, firstEntryPrompt, StringComparison.Ordinal);

        Assert.DoesNotContain(SetStructureHintMarker, laterEntryPrompt, StringComparison.Ordinal);
        Assert.Contains(SetSelectionCountMarker, laterEntryPrompt, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_ReEnteringTheSameMultiSelectField_DoesNotRepeatTheStructureTeaching()
    {
        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");
        var stateTracker = new FormVoiceCommandStateTracker(new Mock<IEvaluationTelemetry>().Object, session);
        var contract = CreateSetContract(("colors", IsSet: true));

        var firstEntryPrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 0, session, stateTracker);
        var reEntryPrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 0, session, stateTracker);

        Assert.Contains(SetStructureHintMarker, firstEntryPrompt, StringComparison.Ordinal);

        Assert.DoesNotContain(SetStructureHintMarker, reEntryPrompt, StringComparison.Ordinal);
        Assert.Contains(SetSelectionCountMarker, reEntryPrompt, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("de")]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("it")]
    public async Task HandleAsync_MultiSelectRecognitionFailureReprompt_ReTeachesStructureAndThenClearsTheMarker(string isoCode)
    {
        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");
        var stateTracker = new FormVoiceCommandStateTracker(new Mock<IEvaluationTelemetry>().Object, session);
        var contract = CreateSetContract(("colors", IsSet: true));
        var structuralHint = FormSetWorkflowPromptBuilder.GetSetAccumulationStructurePrompt(
            contract,
            isoCode,
            new VoiceDialogSettings().GetPolitenessLevel());

        var firstEntryPrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 0, session, stateTracker, isoCode);
        var normalReEntryPrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 0, session, stateTracker, isoCode);
        stateTracker.ArmStructuralTeachingReteach("colors");
        var failureReprompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 0, session, stateTracker, isoCode);
        var afterFailurePrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 0, session, stateTracker, isoCode);

        Assert.Contains(structuralHint, firstEntryPrompt, StringComparison.Ordinal);
        Assert.DoesNotContain(structuralHint, normalReEntryPrompt, StringComparison.Ordinal);
        Assert.Contains(structuralHint, failureReprompt, StringComparison.Ordinal);
        Assert.DoesNotContain(structuralHint, afterFailurePrompt, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_NonMultiSelectField_NeverTeachesSetStructure_AndDoesNotConsumeTheLatch()
    {
        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");
        var stateTracker = new FormVoiceCommandStateTracker(new Mock<IEvaluationTelemetry>().Object, session);
        var contract = CreateSetContract(("name", IsSet: false), ("colors", IsSet: true));

        var nonSetEntryPrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 0, session, stateTracker);
        var setEntryPrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 1, session, stateTracker);

        Assert.DoesNotContain(SetStructureHintMarker, nonSetEntryPrompt, StringComparison.Ordinal);

        Assert.Contains(SetStructureHintMarker, setEntryPrompt, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_NewSession_ReArmsSetAccumulationTeaching()
    {
        // Ein neuer Zustandstracker erlaubt die Einführung auch in der zweiten Sitzung.
        var contract = CreateSetContract(("colors", IsSet: true));

        var firstSession = new EvaluationSession("session-1", EvaluationChannel.Voice, "test-app", "form-1");
        var firstTracker = new FormVoiceCommandStateTracker(new Mock<IEvaluationTelemetry>().Object, firstSession);
        var firstSessionPrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 0, firstSession, firstTracker);

        var secondSession = new EvaluationSession("session-2", EvaluationChannel.Voice, "test-app", "form-1");
        var secondTracker = new FormVoiceCommandStateTracker(new Mock<IEvaluationTelemetry>().Object, secondSession);
        var secondSessionPrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 0, secondSession, secondTracker);

        Assert.Contains(SetStructureHintMarker, firstSessionPrompt, StringComparison.Ordinal);
        Assert.Contains(SetStructureHintMarker, secondSessionPrompt, StringComparison.Ordinal);
    }

    [Fact]
    public void SetPerTurnPrompt_KeepsTheExitAffordance_AndIsNotTheStructureTeaching()
    {
        // Der Bestätigungstext für die laufende Auswahl enthält das Übernahmeangebot ohne die Einführung zur Mehrfachauswahl.
        var contract = CreateMinimalContract();
        var perTurnPrompt = FormSetWorkflowPromptBuilder.GetSetSelectionConfirmPrompt(
            contract,
            "de",
            "Farben",
            "rot, blau",
            PolitenessLevel.Formal);

        Assert.Contains(SetExitCueMarker, perTurnPrompt, StringComparison.Ordinal);
        Assert.DoesNotContain(SetStructureHintMarker, perTurnPrompt, StringComparison.Ordinal);
    }

    // Kennzeichnet die Erklärung des Uhrzeitformats, die im reinen Beispiel fehlt.
    private const string TimeStructureHintMarker = "24-Stunden-Format";

    private const string TimeExamplePhrase = "14:30";

    [Fact]
    public async Task HandleAsync_FirstTimeFieldInSession_TeachesClockConventionWithExample()
    {
        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");
        var stateTracker = new FormVoiceCommandStateTracker(new Mock<IEvaluationTelemetry>().Object, session);
        var contract = CreateTimeContract(("startTime", IsTime: true));

        var entryPrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 0, session, stateTracker);

        Assert.Contains(TimeStructureHintMarker, entryPrompt, StringComparison.Ordinal);
        Assert.Contains(TimeExamplePhrase, entryPrompt, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_LaterTimeFieldInSameSession_SpeaksOnlyExampleWithoutClockHint()
    {
        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");
        var stateTracker = new FormVoiceCommandStateTracker(new Mock<IEvaluationTelemetry>().Object, session);
        var contract = CreateTimeContract(("startTime", IsTime: true), ("endTime", IsTime: true));

        var firstEntryPrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 0, session, stateTracker);
        var laterEntryPrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 1, session, stateTracker);

        Assert.Contains(TimeStructureHintMarker, firstEntryPrompt, StringComparison.Ordinal);

        Assert.DoesNotContain(TimeStructureHintMarker, laterEntryPrompt, StringComparison.Ordinal);
        Assert.Contains(TimeExamplePhrase, laterEntryPrompt, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_ReEnteringTheSameTimeField_DoesNotRepeatTheClockHint()
    {
        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");
        var stateTracker = new FormVoiceCommandStateTracker(new Mock<IEvaluationTelemetry>().Object, session);
        var contract = CreateTimeContract(("startTime", IsTime: true));

        var firstEntryPrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 0, session, stateTracker);
        var reEntryPrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 0, session, stateTracker);

        Assert.Contains(TimeStructureHintMarker, firstEntryPrompt, StringComparison.Ordinal);

        Assert.DoesNotContain(TimeStructureHintMarker, reEntryPrompt, StringComparison.Ordinal);
        Assert.Contains(TimeExamplePhrase, reEntryPrompt, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("de")]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("it")]
    public async Task HandleAsync_TimeRecognitionFailureReprompt_ReTeachesStructureAndThenClearsTheMarker(string isoCode)
    {
        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");
        var stateTracker = new FormVoiceCommandStateTracker(new Mock<IEvaluationTelemetry>().Object, session);
        var contract = CreateTimeContract(("startTime", IsTime: true));
        var structuralHint = FormTimeEntryExamplePromptBuilder.GetTimeEntryStructureHintPrompt(
            contract,
            isoCode,
            new VoiceDialogSettings().GetPolitenessLevel());

        var firstEntryPrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 0, session, stateTracker, isoCode);
        var normalReEntryPrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 0, session, stateTracker, isoCode);
        stateTracker.ArmStructuralTeachingReteach("startTime");
        var failureReprompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 0, session, stateTracker, isoCode);
        var afterFailurePrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 0, session, stateTracker, isoCode);

        Assert.False(string.IsNullOrWhiteSpace(structuralHint));
        Assert.Contains(structuralHint!, firstEntryPrompt, StringComparison.Ordinal);
        Assert.DoesNotContain(structuralHint!, normalReEntryPrompt, StringComparison.Ordinal);
        Assert.Contains(structuralHint!, failureReprompt, StringComparison.Ordinal);
        Assert.DoesNotContain(structuralHint!, afterFailurePrompt, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_NonTimeField_NeverTeachesTimeStructure_AndDoesNotConsumeTheLatch()
    {
        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");
        var stateTracker = new FormVoiceCommandStateTracker(new Mock<IEvaluationTelemetry>().Object, session);
        var contract = CreateTimeContract(("name", IsTime: false), ("startTime", IsTime: true));

        var nonTimeEntryPrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 0, session, stateTracker);
        var timeEntryPrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 1, session, stateTracker);

        Assert.DoesNotContain(TimeStructureHintMarker, nonTimeEntryPrompt, StringComparison.Ordinal);

        Assert.Contains(TimeStructureHintMarker, timeEntryPrompt, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_NewSession_ReArmsTimeStructureTeaching()
    {
        // Ein neuer Zustandstracker erlaubt die Uhrzeiterklärung auch in der zweiten Sitzung.
        var contract = CreateTimeContract(("startTime", IsTime: true));

        var firstSession = new EvaluationSession("session-1", EvaluationChannel.Voice, "test-app", "form-1");
        var firstTracker = new FormVoiceCommandStateTracker(new Mock<IEvaluationTelemetry>().Object, firstSession);
        var firstSessionPrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 0, firstSession, firstTracker);

        var secondSession = new EvaluationSession("session-2", EvaluationChannel.Voice, "test-app", "form-1");
        var secondTracker = new FormVoiceCommandStateTracker(new Mock<IEvaluationTelemetry>().Object, secondSession);
        var secondSessionPrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 0, secondSession, secondTracker);

        Assert.Contains(TimeStructureHintMarker, firstSessionPrompt, StringComparison.Ordinal);
        Assert.Contains(TimeStructureHintMarker, secondSessionPrompt, StringComparison.Ordinal);
    }

    private const string SpellingOverrideMarker = "buchstabieren";

    [Fact]
    public async Task HandleAsync_FirstSpellableFieldInSession_AnnouncesTheSpellingOverrideAtEntry()
    {
        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");
        var stateTracker = new FormVoiceCommandStateTracker(new Mock<IEvaluationTelemetry>().Object, session);
        var contract = CreateTypedContract(("note", """{"kind":"string"}""", CodeStyle: false));

        var entryPrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 0, session, stateTracker);

        Assert.Contains(SpellingOverrideMarker, entryPrompt, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_FirstSpellableFieldIsCode_AlsoAnnouncesTheSpellingOverride()
    {
        // Auch ein Codefeld darf den Wechsel zum Buchstabiermodus anbieten.
        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");
        var stateTracker = new FormVoiceCommandStateTracker(new Mock<IEvaluationTelemetry>().Object, session);
        var contract = CreateTypedContract(("iban", """{"kind":"string"}""", CodeStyle: true));

        var entryPrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 0, session, stateTracker);

        Assert.Contains(SpellingOverrideMarker, entryPrompt, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_LaterSpellableFieldInSameSession_DoesNotRepeatTheSpellingOverride()
    {
        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");
        var stateTracker = new FormVoiceCommandStateTracker(new Mock<IEvaluationTelemetry>().Object, session);
        var contract = CreateTypedContract(
            ("note", """{"kind":"string"}""", CodeStyle: false),
            ("comment", """{"kind":"string"}""", CodeStyle: false));

        var firstEntryPrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 0, session, stateTracker);
        var laterEntryPrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 1, session, stateTracker);

        Assert.Contains(SpellingOverrideMarker, firstEntryPrompt, StringComparison.Ordinal);
        Assert.DoesNotContain(SpellingOverrideMarker, laterEntryPrompt, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_ReEnteringTheSameSpellableField_DoesNotRepeatTheSpellingOverride()
    {
        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");
        var stateTracker = new FormVoiceCommandStateTracker(new Mock<IEvaluationTelemetry>().Object, session);
        var contract = CreateTypedContract(("note", """{"kind":"string"}""", CodeStyle: false));

        var firstEntryPrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 0, session, stateTracker);
        var reEntryPrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 0, session, stateTracker);

        Assert.Contains(SpellingOverrideMarker, firstEntryPrompt, StringComparison.Ordinal);
        Assert.DoesNotContain(SpellingOverrideMarker, reEntryPrompt, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("""{"kind":"boolean"}""")]
    [InlineData("""{"kind":"integer"}""")]
    [InlineData("""{"kind":"enum","values":[{"code":"x","displayName":{"de":"X"}}]}""")]
    public async Task HandleAsync_NonSpellableField_NeverAnnouncesSpellingOverride_AndDoesNotConsumeTheLatch(string valueTypeJson)
    {
        // Die drei Fälle bieten Buchstabieren nicht an; der Hinweis bleibt für das folgende Textfeld verfügbar.
        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");
        var stateTracker = new FormVoiceCommandStateTracker(new Mock<IEvaluationTelemetry>().Object, session);
        var contract = CreateTypedContract(
            ("choice", valueTypeJson, CodeStyle: false),
            ("note", """{"kind":"string"}""", CodeStyle: false));

        var nonSpellableEntryPrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 0, session, stateTracker);
        var spellableEntryPrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 1, session, stateTracker);

        Assert.DoesNotContain(SpellingOverrideMarker, nonSpellableEntryPrompt, StringComparison.Ordinal);

        Assert.Contains(SpellingOverrideMarker, spellableEntryPrompt, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleAsync_NewSession_ReArmsSpellingOverrideAnnouncement()
    {
        // Ein neuer Zustandstracker erlaubt den Buchstabierhinweis auch in der zweiten Sitzung.
        var contract = CreateTypedContract(("note", """{"kind":"string"}""", CodeStyle: false));

        var firstSession = new EvaluationSession("session-1", EvaluationChannel.Voice, "test-app", "form-1");
        var firstTracker = new FormVoiceCommandStateTracker(new Mock<IEvaluationTelemetry>().Object, firstSession);
        var firstSessionPrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 0, firstSession, firstTracker);

        var secondSession = new EvaluationSession("session-2", EvaluationChannel.Voice, "test-app", "form-1");
        var secondTracker = new FormVoiceCommandStateTracker(new Mock<IEvaluationTelemetry>().Object, secondSession);
        var secondSessionPrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 0, secondSession, secondTracker);

        Assert.Contains(SpellingOverrideMarker, firstSessionPrompt, StringComparison.Ordinal);
        Assert.Contains(SpellingOverrideMarker, secondSessionPrompt, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(PrivacyClass.Sensitive)]
    public async Task HandleAsync_ProtectionClassSpellableField_NeverAnnouncesSpellingOverride_AndDoesNotConsumeTheLatch(
        PrivacyClass privacyClass)
    {
        // Das sensible Textfeld bietet Buchstabieren nicht an und verbraucht den einmaligen Hinweis nicht.
        // Der Hinweis bleibt für das folgende ungeschützte Textfeld verfügbar.
        // Geheime Felder sind hier ausgenommen: Für sie wird die Spracheingabe bereits vor der Aufnahme gesperrt.
        var session = new EvaluationSession("test-session", EvaluationChannel.Voice, "test-app", "form-1");
        var stateTracker = new FormVoiceCommandStateTracker(new Mock<IEvaluationTelemetry>().Object, session);
        var contract = CreateTypedContract(
            ("secret", """{"kind":"string"}""", CodeStyle: false, privacyClass),
            ("note", """{"kind":"string"}""", CodeStyle: false, (PrivacyClass?)null));

        var protectionClassEntryPrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 0, session, stateTracker);
        var spellableEntryPrompt = await EnterFieldAndCaptureEntryPromptAsync(contract, fieldIndex: 1, session, stateTracker);

        Assert.DoesNotContain(SpellingOverrideMarker, protectionClassEntryPrompt, StringComparison.Ordinal);

        Assert.Contains(SpellingOverrideMarker, spellableEntryPrompt, StringComparison.Ordinal);
    }

    private static FormContract CreateTypedContract(params (string FieldId, string ValueTypeJson, bool CodeStyle)[] fields)
        => CreateTypedContract(fields.Select(f => (f.FieldId, f.ValueTypeJson, f.CodeStyle, (PrivacyClass?)null)).ToArray());

    private static FormContract CreateTypedContract(params (string FieldId, string ValueTypeJson, bool CodeStyle, PrivacyClass? PrivacyClass)[] fields)
    {
        var variables = fields
            .Select(spec => new VariableDefinition
            {
                VariableId = VariableId.From(spec.FieldId),
                Name = spec.FieldId,
                ValueType = JsonDocument.Parse(spec.ValueTypeJson).RootElement,
                PrivacyClass = spec.PrivacyClass
            })
            .ToList();

        var fieldDefinitions = fields
            .Select(spec => new FieldDefinition
            {
                FieldId = FieldId.From(spec.FieldId),
                VariableId = VariableId.From(spec.FieldId),
                Label = new LocalizedText { ["de"] = spec.FieldId },
                CodeStyle = spec.CodeStyle,
                Editability = FieldEditability.UserEditable
            })
            .ToList();

        var members = fields
            .Select(spec => new FieldGroupMember { Kind = "field", FieldId = FieldId.From(spec.FieldId) })
            .ToList();

        return new FormContract
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
            Variables = variables,
            Fields = fieldDefinitions,
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("main"),
                    Title = new LocalizedText { ["de"] = "Main" },
                    Members = members
                }
            ]
        };
    }

    // Öffnet das gewählte Feld mit neuem Interpreter und Handler, aber dem übergebenen Zustandstracker.
    // Der Sprachmock erfasst den Text vor der Aufnahme und beendet sie mit NoSpeechDetectedException.
    private static async Task<string> EnterFieldAndCaptureEntryPromptAsync(
        FormContract contract,
        int fieldIndex,
        EvaluationSession session,
        FormVoiceCommandStateTracker stateTracker,
        string isoCode = "de",
        bool allowBargeIn = false)
    {
        var capturedPrompts = new List<string>();
        var telemetry = new Mock<IEvaluationTelemetry>();

        var speechService = new Mock<IFormVoiceCommandSpeechService>();
        speechService.ForwardConfirmationCaptureToValueCapture();
        speechService
            .Setup(s => s.CaptureTranscriptAsync(
                It.IsAny<string>(),
                isoCode,
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, string, FormVoiceCommandCaptureSettings, Action, SpeechCaptureTelemetryContext?, RecognitionProfile, SpeechKeyterms?, CancellationToken>(
                (prompt, _, settings, onStartListening, _, _, _, _) =>
                {
                    Assert.Equal(allowBargeIn, settings.VoiceSettings.GetAllowBargeIn());
                    capturedPrompts.Add(prompt);
                    onStartListening();
                    throw new NoSpeechDetectedException();
                });

        var submissionService = new Mock<IFormVoiceCommandSubmissionService>();
        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>();
        router
            .Setup(r => r.TryHandleAsync(
                CommandId.From(GlobalCommandIds.Next),
                It.IsAny<FormVoiceCommandGlobalCommandContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(VoiceCommandDialogResult.BackToFormSelection(
                "app",
                "current",
                ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase)));

        var handler = CreateFieldInteractionHandler(
            telemetry.Object,
            speechService.Object,
            submissionService.Object,
            router.Object);

        var interpreter = new FormInterpreterRuntimeOwner(
            contract,
            requestedIsoCode: isoCode,
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));
        for (var i = 0; i < fieldIndex; i++)
        {
            Assert.True(await interpreter.TryMoveFocusNextAsync());
        }

        var field = await interpreter.GetFocusedFieldAsync();
        var context = CreateContext(contract, isoCode, new VoiceDialogSettings { MaxRetries = 0, AllowBargeIn = allowBargeIn });
        var autoHelp = new FormVoiceCommandAutoHelpTracker(
            context.VoiceSettings,
            new Mock<IFormVoiceCommandPromptService>().Object,
            new Mock<IFormVoiceCommandDialogStateTracker>().Object,
            interpreter,
            context.IsoCode);

        await handler.HandleAsync(
            context,
            interpreter,
            field,
            session,
            stateTracker,
            autoHelp,
            CancellationToken.None);

        return Assert.Single(capturedPrompts);
    }

    private static FormContract CreateDateContract(params (string FieldId, bool IsDate)[] fields)
    {
        var variables = fields
            .Select(spec => new VariableDefinition
            {
                VariableId = VariableId.From(spec.FieldId),
                Name = spec.FieldId,
                ValueType = JsonDocument.Parse(spec.IsDate ? """{"kind":"date"}""" : """{"kind":"string"}""").RootElement
            })
            .ToList();

        var fieldDefinitions = fields
            .Select(spec => new FieldDefinition
            {
                FieldId = FieldId.From(spec.FieldId),
                VariableId = VariableId.From(spec.FieldId),
                Label = CreateLocalizedText(spec.FieldId),
                Editability = FieldEditability.UserEditable
            })
            .ToList();

        var members = fields
            .Select(spec => new FieldGroupMember { Kind = "field", FieldId = FieldId.From(spec.FieldId) })
            .ToList();

        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "app",
            FormId = FormId.From("current"),
            DisplayName = CreateLocalizedText("Teil 1"),
            SupportedLanguages = [.. SupportedEntryPromptIsoCodes],
            DefaultLanguage = "de",
            StrictLocalization = true,
            EntryFieldGroupId = FieldGroupId.From("main"),
            Variables = variables,
            Fields = fieldDefinitions,
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("main"),
                    Title = CreateLocalizedText("Main"),
                    Members = members
                }
            ]
        };
    }

    private static FormContract CreateSetContract(params (string FieldId, bool IsSet)[] fields)
    {
        var variables = fields
            .Select(spec => new VariableDefinition
            {
                VariableId = VariableId.From(spec.FieldId),
                Name = spec.FieldId,
                ValueType = JsonDocument.Parse(spec.IsSet
                    ? """{"kind":"set","minSelected":2,"maxSelected":4}"""
                    : """{"kind":"string"}""").RootElement
            })
            .ToList();

        var fieldDefinitions = fields
            .Select(spec => new FieldDefinition
            {
                FieldId = FieldId.From(spec.FieldId),
                VariableId = VariableId.From(spec.FieldId),
                Label = CreateLocalizedText(spec.FieldId),
                Editability = FieldEditability.UserEditable
            })
            .ToList();

        var members = fields
            .Select(spec => new FieldGroupMember { Kind = "field", FieldId = FieldId.From(spec.FieldId) })
            .ToList();

        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "app",
            FormId = FormId.From("current"),
            DisplayName = CreateLocalizedText("Teil 1"),
            SupportedLanguages = [.. SupportedEntryPromptIsoCodes],
            DefaultLanguage = "de",
            StrictLocalization = true,
            EntryFieldGroupId = FieldGroupId.From("main"),
            Variables = variables,
            Fields = fieldDefinitions,
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("main"),
                    Title = CreateLocalizedText("Main"),
                    Members = members
                }
            ]
        };
    }

    private static FormContract CreateTimeContract(params (string FieldId, bool IsTime)[] fields)
    {
        var variables = fields
            .Select(spec => new VariableDefinition
            {
                VariableId = VariableId.From(spec.FieldId),
                Name = spec.FieldId,
                ValueType = JsonDocument.Parse(spec.IsTime ? """{"kind":"time"}""" : """{"kind":"string"}""").RootElement
            })
            .ToList();

        var fieldDefinitions = fields
            .Select(spec => new FieldDefinition
            {
                FieldId = FieldId.From(spec.FieldId),
                VariableId = VariableId.From(spec.FieldId),
                Label = CreateLocalizedText(spec.FieldId),
                Editability = FieldEditability.UserEditable
            })
            .ToList();

        var members = fields
            .Select(spec => new FieldGroupMember { Kind = "field", FieldId = FieldId.From(spec.FieldId) })
            .ToList();

        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "app",
            FormId = FormId.From("current"),
            DisplayName = CreateLocalizedText("Teil 1"),
            SupportedLanguages = [.. SupportedEntryPromptIsoCodes],
            DefaultLanguage = "de",
            StrictLocalization = true,
            EntryFieldGroupId = FieldGroupId.From("main"),
            Variables = variables,
            Fields = fieldDefinitions,
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("main"),
                    Title = CreateLocalizedText("Main"),
                    Members = members
                }
            ]
        };
    }

    // Der Testvertrag stellt ein vorbelegtes, schreibgeschütztes Hinweisfeld vor das bearbeitbare Namensfeld.
    // Er wird lokal aufgebaut und hängt damit nicht von einem veröffentlichten Formular ab.
    private static FormContract CreateGuidedNamePairContract()
    {
        var stringValueType = JsonDocument.Parse("""{"kind":"string"}""").RootElement;

        return new FormContract
        {
            SchemaVersion = "1",
            ContractVersion = "1",
            ApplicationId = "app",
            FormId = FormId.From("current"),
            DisplayName = new LocalizedText { ["de"] = "Teil 1" },
            SupportedLanguages = ["de"],
            DefaultLanguage = "de",
            StrictLocalization = true,
            EntryFieldGroupId = FieldGroupId.From("name"),
            Variables =
            [
                new VariableDefinition
                {
                    VariableId = VariableId.From("guidanceName"),
                    Name = "guidanceName",
                    ValueType = stringValueType,
                    InitialValue = JsonDocument.Parse("\"Marta\"").RootElement
                },
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
                    FieldId = FieldId.From("guidanceNameField"),
                    VariableId = VariableId.From("guidanceName"),
                    Label = new LocalizedText { ["de"] = "Vorname. Der vorgegebene Wert lautet: Marta." },
                    Editability = FieldEditability.ReadOnly
                },
                new FieldDefinition
                {
                    FieldId = FieldId.From("nameField"),
                    VariableId = VariableId.From("name"),
                    Label = new LocalizedText { ["de"] = "Vorname" },
                    Editability = FieldEditability.UserEditable
                }
            ],
            FieldGroups =
            [
                new FieldGroupDefinition
                {
                    FieldGroupId = FieldGroupId.From("name"),
                    Title = new LocalizedText { ["de"] = "Schritt 1: Vorname" },
                    Members =
                    [
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("guidanceNameField") },
                        new FieldGroupMember { Kind = "field", FieldId = FieldId.From("nameField") }
                    ]
                }
            ]
        };
    }

    private static FormContract CreateTwoGuidedGroupsContract()
    {
        var contract = CreateGuidedNamePairContract();
        var stringType = JsonDocument.Parse("""{"kind":"string"}""").RootElement;
        contract.Variables.Add(new VariableDefinition { VariableId = VariableId.From("nextGuidance"), Name = "nextGuidance", ValueType = stringType, InitialValue = JsonDocument.Parse("\"Lea\"").RootElement });
        contract.Variables.Add(new VariableDefinition { VariableId = VariableId.From("next"), Name = "next", ValueType = stringType });
        contract.Fields.Add(new FieldDefinition { FieldId = FieldId.From("nextGuidanceField"), VariableId = VariableId.From("nextGuidance"), Label = new LocalizedText { ["de"] = "Zweiter Hinweis" }, Editability = FieldEditability.ReadOnly });
        contract.Fields.Add(new FieldDefinition { FieldId = FieldId.From("nextField"), VariableId = VariableId.From("next"), Label = new LocalizedText { ["de"] = "Zweites Feld" }, Editability = FieldEditability.UserEditable });
        contract.FieldGroups.Add(new FieldGroupDefinition
        {
            FieldGroupId = FieldGroupId.From("nextGroup"), Title = new LocalizedText { ["de"] = "Zweiter Schritt" },
            Members = [new FieldGroupMember { Kind = "field", FieldId = FieldId.From("nextGuidanceField") }, new FieldGroupMember { Kind = "field", FieldId = FieldId.From("nextField") }]
        });
        contract.EntryFieldGroupId = FieldGroupId.From("root");
        contract.FieldGroups.Add(new FieldGroupDefinition
        {
            FieldGroupId = FieldGroupId.From("root"), Title = new LocalizedText { ["de"] = "Beide Schritte" },
            Members = [new FieldGroupMember { Kind = "fieldGroup", FieldGroupId = FieldGroupId.From("name") }, new FieldGroupMember { Kind = "fieldGroup", FieldGroupId = FieldGroupId.From("nextGroup") }]
        });
        return contract;
    }

    private static FormContract CreateMinimalContract(
        IReadOnlyList<string>? preferredInputModes = null,
        PrivacyClass? privacyClass = null,
        SensitiveDataPolicy? sensitiveDataPolicy = null,
        string? formatHint = null)
    {
        return new FormContract
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
                    ValueType = JsonDocument.Parse(formatHint is null ? """{"kind":"string"}""" : $$"""{"kind":"string","formatHint":"{{formatHint}}"}""").RootElement,
                    PrivacyClass = privacyClass,
                    SensitiveDataPolicy = sensitiveDataPolicy
                }
            ],
            Fields =
            [
                new FieldDefinition
                {
                    FieldId = FieldId.From("name"),
                    VariableId = VariableId.From("name"),
                    Label = new LocalizedText { ["de"] = "Name" },
                    PreferredInputModes = preferredInputModes?.ToList(),
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

    private static FormVoiceCommandFieldInteractionHandler CreateFieldInteractionHandler(
        IEvaluationTelemetry telemetry,
        IFormVoiceCommandSpeechService speechService,
        IFormVoiceCommandSubmissionService submissionService,
        IFormVoiceCommandGlobalCommandRouter commandRouter)
    {
        var commandExecutionService = new FormVoiceCommandGlobalCommandExecutionService(
            telemetry,
            commandRouter);
        var replayBuffer = new FormVoiceCommandSpokenOutputReplayBuffer();
        var errorHandlingService = new FormVoiceCommandFieldErrorHandlingService(
            telemetry,
            speechService,
            replayBuffer,
            new CaptureReplayPolicy());
        var parser = new FormTranscriptParser(new YesNoTranscriptClassifier(), new ModeledNegationParticleResolver());
        var digitsWorkflowService = CreateDigitsWorkflowService(telemetry, speechService, parser);
        var ambiguityTelemetry = CreateInputVsCommandTelemetry(telemetry);
        var transcriptOrchestrator = new FormVoiceCommandFieldTranscriptOrchestrator(
            new FormVoiceCommandFieldTranscriptCapture(speechService, errorHandlingService),
            new FormVoiceCommandTranscriptInterpretationService(),
            new ParsedCommandBranch(
                commandExecutionService,
                errorHandlingService,
                ambiguityTelemetry,
                new FormVoiceCommandConfirmationDecisionService(
                    new FormVoiceCommandConfirmationTranscriptCapture(speechService),
                    new FormVoiceCommandConfirmationTranscriptInterpreter(
                        new FormVoiceCommandConfirmationCommandDecisionMapper(),
                        parser),
                    new FormVoiceCommandConfirmationMissingDecisionPolicy()),
                telemetry),
            new FormVoiceCommandFieldEntryDependencies(
                new FormVoiceCommandInputAvailabilityGuard(errorHandlingService),
                new FormVoiceCommandConfirmationTranscriptCapture(speechService),
                new FormVoiceCommandReadOnlyFieldSpeaker(speechService, replayBuffer)),
            new FormVoiceCommandDateComponentTranscriptBranch(
                new DateComponentEntryWorkflowDriver(speechService, parser, parser)),
            new FormVoiceCommandDigitsTranscriptBranch(digitsWorkflowService),
            new FormVoiceCommandSpellingTranscriptBranch(CreateSpellingWorkflowService(telemetry, speechService, parser)),
            new FormVoiceCommandSetTranscriptBranch(
                new FormVoiceCommandSetWorkflowService(
                    new CharacterAccumulationWorkflowDriver(NullLogger<CharacterAccumulationWorkflowDriver>.Instance),
                    AnchoredCommandFlowTestFactory.Create(),
                    new SetWorkflowCommandResolver(new SetWorkflowGlobalCommandMapper()),
                    new SetWorkflowCommandActionFlow(telemetry),
                    new SetWorkflowInputRejectionFlow(new WorkflowUnrecognizedInputAnnouncer(telemetry, speechService)),
                    parser,
                    speechService)),
            new FormVoiceCommandAmbiguityTranscriptBranch(
                new FormVoiceCommandAmbiguityResolver(),
                errorHandlingService,
                ambiguityTelemetry));
        var fieldApplyService = new FormVoiceCommandFieldApplyService(
            telemetry,
            telemetry,
            errorHandlingService,
            new FormVoiceCommandConfirmationHandler(
                new FormVoiceCommandConfirmationValuePresenter(
                    speechService,
                    new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance)),
                new FormVoiceCommandConfirmationDecisionService(
                    new FormVoiceCommandConfirmationTranscriptCapture(speechService),
                    new FormVoiceCommandConfirmationTranscriptInterpreter(
                        new FormVoiceCommandConfirmationCommandDecisionMapper(),
                        parser),
                    new FormVoiceCommandConfirmationMissingDecisionPolicy()),
                new FormVoiceCommandConfirmationRepairActionFlow(telemetry, speechService, commandExecutionService),
                new FormVoiceCommandConfirmedFieldCompletionFlow(
                    speechService,
                    Mock.Of<IEarconPlayer>(),
                    new FormClosingReviewTurnFlow(speechService, commandExecutionService)),
                errorHandlingService),
            new FormVoiceCommandConfirmationValuePresenter(
                speechService,
                new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance)),
            new FormVoiceCommandAdditionalFieldProposalHandler(
                telemetry,
                new FormVoiceCommandConfirmationTranscriptCapture(speechService),
                new FormTranscriptParser(new YesNoTranscriptClassifier(), new ModeledNegationParticleResolver()),
                speechService),
            new FormVoiceCommandSpellingEscalationFlow(
                new SpellingConfidenceEscalationPolicy(),
                new FormVoiceCommandConfirmationValuePresenter(
                    speechService,
                    new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance)),
                new FormVoiceCommandConfirmationDecisionService(
                    new FormVoiceCommandConfirmationTranscriptCapture(speechService),
                    new FormVoiceCommandConfirmationTranscriptInterpreter(
                        new FormVoiceCommandConfirmationCommandDecisionMapper(),
                        parser),
                    new FormVoiceCommandConfirmationMissingDecisionPolicy()),
                new FormVoiceCommandConfirmationRepairActionFlow(telemetry, speechService, commandExecutionService),
                Mock.Of<IEarconPlayer>()),
            new FormRepairReasonResolver());

        return new FormVoiceCommandFieldInteractionHandler(
            telemetry,
            speechService,
            transcriptOrchestrator,
            fieldApplyService,
            replayBuffer);
    }

    private static FormVoiceCommandConfirmationDecisionService CreateConfirmationDecisionService(
        IFormVoiceCommandSpeechService speechService,
        IFormTranscriptParser parser)
        => new(
            new FormVoiceCommandConfirmationTranscriptCapture(speechService),
            new FormVoiceCommandConfirmationTranscriptInterpreter(
                new FormVoiceCommandConfirmationCommandDecisionMapper(),
                parser),
            new FormVoiceCommandConfirmationMissingDecisionPolicy());

    private static FormVoiceCommandInputVsCommandTelemetry CreateInputVsCommandTelemetry(
        IEvaluationTelemetry telemetry)
        => new(telemetry, new InputVsCommandResolutionStrategyMapper());

    private static FormVoiceCommandDigitsWorkflowService CreateDigitsWorkflowService(
        IEvaluationTelemetry telemetry,
        IFormVoiceCommandSpeechService speechService,
        IFormTranscriptParser parser)
    {
        var transcriptCapture = new DigitsWorkflowTranscriptCapture(speechService);
        return new FormVoiceCommandDigitsWorkflowService(
            new CharacterAccumulationWorkflowDriver(NullLogger<CharacterAccumulationWorkflowDriver>.Instance),
            AnchoredCommandFlowTestFactory.Create(),
            parser,
            CreateDigitsWorkflowCommandResolver(),
            new DigitsWorkflowCommandActionFlow(
                telemetry,
                speechService,
                new DigitsWorkflowNoValuePrompt(speechService)),
            new DigitsWorkflowInputRejectionFlow(
                new WorkflowUnrecognizedInputAnnouncer(telemetry, speechService)),
            transcriptCapture);
    }

    private static DigitsWorkflowCommandResolver CreateDigitsWorkflowCommandResolver()
        => new(
            new DigitsWorkflowLocalCommandMapper(),
            new DigitsWorkflowGlobalCommandMapper());

    private static FormVoiceCommandSpellingWorkflowService CreateSpellingWorkflowService(
        IEvaluationTelemetry telemetry,
        IFormVoiceCommandSpeechService speechService,
        IFormTranscriptParser parser)
        => new(
            new CharacterAccumulationWorkflowDriver(NullLogger<CharacterAccumulationWorkflowDriver>.Instance),
            AnchoredCommandFlowTestFactory.Create(),
            new SpellingWorkflowCommandResolver(
                new SpellingWorkflowLocalCommandMapper(),
                new SpellingWorkflowGlobalCommandMapper()),
            parser,
            speechService,
            new SpellingWorkflowCommandActionFlow(
                telemetry,
                speechService,
                new SpellingWorkflowNoValuePrompt(speechService)),
            new SpellingWorkflowInputRejectionFlow(
                new WorkflowUnrecognizedInputAnnouncer(telemetry, speechService)),
            new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance));

    private static FormVoiceCommandContext CreateContext(
        FormContract contract,
        string isoCode = "de",
        VoiceDialogSettings? voiceSettings = null)
    {
        var form = new FormDescriptor
        {
            FormId = FormId.From("current"),
            DisplayName = CreateLocalizedText("Teil 1"),
            Description = CreateLocalizedText("Teil 1"),
            LatestContractVersion = "1",
            AvailableContractVersions = ["1"]
        };

        var application = new ApplicationDescriptor
        {
            ApplicationId = "app",
            DisplayName = CreateLocalizedText("App"),
            Description = CreateLocalizedText("App"),
            SupportedLanguages = [.. SupportedEntryPromptIsoCodes],
            DefaultLanguage = "de",
            Forms = [form]
        };

        return new FormVoiceCommandContext(
            SessionIdentity: new FormVoiceCommandSessionIdentity(
                IsoCode: isoCode,
                ApplicationId: "app",
                FormId: "current",
                WakeWordActivationForms: ["okay"]),
            Selection: new FormVoiceCommandSelectionContext(
                VoiceSettings: voiceSettings ?? new VoiceDialogSettings(),
                Application: application,
                CompletedFormIds: ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase),
                Form: form,
                Contract: contract),
            Values: new FormVoiceCommandValueSnapshot(
                ValuesByVariableId: ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase),
                SharedAdoptions: []));
    }

    private static LocalizedText CreateLocalizedText(string value)
    {
        var text = new LocalizedText();
        foreach (var isoCode in SupportedEntryPromptIsoCodes)
        {
            text[isoCode] = value;
        }

        return text;
    }

    private static FormSurfaceDefinition CreateFormProjection(params (string FieldGroupId, string? VoiceSummary)[] fieldGroups)
    {
        var fieldGroupsById = fieldGroups.ToDictionary(
            fieldGroup => fieldGroup.FieldGroupId,
            fieldGroup => new FormSurfaceFieldGroupInfo(
                fieldGroup.FieldGroupId,
                title: fieldGroup.FieldGroupId,
                voiceSummary: fieldGroup.VoiceSummary),
            StringComparer.OrdinalIgnoreCase);

        return new FormSurfaceDefinition("main", fieldGroupsById, []);
    }
}
