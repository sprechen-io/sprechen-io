using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormVoiceCommandFieldTranscriptOrchestratorTests
{
    [Fact]
    public async Task OrchestrateAsync_ProtectedSpellingInitialCapture_IsCueFree()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(s => s.CaptureTranscriptAsync(
                It.IsAny<string>(),
                "de",
                It.Is<FormVoiceCommandCaptureSettings>(settings =>
                    settings.InputAcceptanceFeedback == SpeechInputAcceptanceFeedback.CueFree),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                RecognitionProfile.Spelling,
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NoSpeechDetectedException());

        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        var orchestrator = CreateOrchestrator(telemetry.Object, speechService.Object, router.Object);
        var contract = CreateMinimalContract(
            PrivacyClass.Sensitive,
            new SensitiveDataPolicy
            {
                ReadbackPolicy = SensitiveDataPolicyDefaults.ReadbackOptInRequired,
            });
        var interpreter = new FormInterpreterRuntimeOwner(
            contract,
            requestedIsoCode: "de",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));
        var context = CreateContext(contract);
        var field = await interpreter.GetFocusedFieldAsync();
        var session = new EvaluationSession("protected-spelling", EvaluationChannel.Voice, "app", "app/current");
        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        stateTracker.BeginFieldPrompt(field.FieldGroupId);

        var result = await orchestrator.OrchestrateAsync(
            context,
            interpreter,
            field,
            session,
            stateTracker,
            CreateAutoHelpTracker(interpreter),
            expectedInputMode: InputMode.Spelling,
            CancellationToken.None);

        Assert.True(result.HasFinalResult);
        router.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task OrchestrateAsync_PublicSpellingInitialCapture_DefersFeedbackToSemanticReadback()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(s => s.CaptureTranscriptAsync(
                It.IsAny<string>(),
                "de",
                It.Is<FormVoiceCommandCaptureSettings>(settings =>
                    settings.InputAcceptanceFeedback == SpeechInputAcceptanceFeedback.PostInterpretation),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                RecognitionProfile.Spelling,
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NoSpeechDetectedException());

        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        var orchestrator = CreateOrchestrator(telemetry.Object, speechService.Object, router.Object);
        var contract = CreateMinimalContract();
        var interpreter = new FormInterpreterRuntimeOwner(
            contract,
            requestedIsoCode: "de",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));
        var context = CreateContext(contract);
        var field = await interpreter.GetFocusedFieldAsync();
        var session = new EvaluationSession("public-spelling", EvaluationChannel.Voice, "app", "app/current");
        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        stateTracker.BeginFieldPrompt(field.FieldGroupId);

        var result = await orchestrator.OrchestrateAsync(
            context,
            interpreter,
            field,
            session,
            stateTracker,
            CreateAutoHelpTracker(interpreter),
            expectedInputMode: InputMode.Spelling,
            CancellationToken.None);

        Assert.True(result.HasFinalResult);
        router.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task OrchestrateAsync_WhenNoSpeechDetected_ReturnsCompletedContinue()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
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
            .ThrowsAsync(new NoSpeechDetectedException());

        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        var orchestrator = CreateOrchestrator(telemetry.Object, speechService.Object, router.Object);

        var contract = CreateMinimalContract();
        var interpreter = new FormInterpreterRuntimeOwner(
            contract,
            requestedIsoCode: "de",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));
        var context = CreateContext(contract);
        var field = await interpreter.GetFocusedFieldAsync();
        var session = new EvaluationSession("session-1", EvaluationChannel.Voice, "app", "app/current");
        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        stateTracker.BeginFieldPrompt(field.FieldGroupId);
        var autoHelp = CreateAutoHelpTracker(interpreter);

        var result = await orchestrator.OrchestrateAsync(
            context,
            interpreter,
            field,
            session,
            stateTracker,
            autoHelp,
            expectedInputMode: InputMode.Voice,
            CancellationToken.None);

        Assert.True(result.HasFinalResult);
        Assert.True(result.FinalResult!.ShouldContinue);
        Assert.False(result.FinalResult.WasSubmitted);
        Assert.Null(result.FinalResult.DialogResult);

        telemetry.Verify(t => t.TrackFieldAttempt(session, field.FieldId, InputModes.Voice, false, null), Times.Once);
        telemetry.Verify(t => t.TrackConstraintViolation(
            session,
            field.FieldId,
            FormVoiceCommandFieldErrorCodes.NoSpeechDetected,
            SeverityLevels.Warning,
            TelemetryErrorTriggers.SystemRecognition), Times.Once);
        telemetry.Verify(t => t.TrackRepairAttempt(
            session,
            field.FieldId,
            FormVoiceCommandFieldErrorCodes.NoSpeechDetected), Times.Once);

        router.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task OrchestrateAsync_WhenCommandPrefixAndKnownCommand_ReturnsCommandResult()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
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
                return Task.FromResult<SpeechCaptureResult>(new SpeechCaptureResult("kommando weiter", null));
            });

        var expectedDialogResult = VoiceCommandDialogResult.BackToFormSelection(
            "app",
            "current",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));
        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        router
            .Setup(r => r.TryHandleAsync(
                CommandId.From(GlobalCommandIds.Next),
                It.IsAny<FormVoiceCommandGlobalCommandContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDialogResult);

        var orchestrator = CreateOrchestrator(telemetry.Object, speechService.Object, router.Object);

        var contract = CreateMinimalContract();
        var interpreter = new FormInterpreterRuntimeOwner(
            contract,
            requestedIsoCode: "de",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));
        var context = CreateContext(contract);
        var field = await interpreter.GetFocusedFieldAsync();
        var session = new EvaluationSession("session-2", EvaluationChannel.Voice, "app", "app/current");
        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        stateTracker.BeginFieldPrompt(field.FieldGroupId);
        var autoHelp = CreateAutoHelpTracker(interpreter);

        var result = await orchestrator.OrchestrateAsync(
            context,
            interpreter,
            field,
            session,
            stateTracker,
            autoHelp,
            expectedInputMode: InputMode.Voice,
            CancellationToken.None);

        Assert.True(result.HasFinalResult);
        Assert.Same(expectedDialogResult, result.FinalResult!.DialogResult);
        telemetry.Verify(t => t.TrackGlobalCommand(session, GlobalCommandIds.Next), Times.Once);
        telemetry.Verify(t => t.TrackAmbiguityResolved(
            session,
            "input_vs_command",
            "context_default",
            CommandSignalClass.Clear,
            "accepted",
            field.FieldId,
            GlobalCommandIds.Next), Times.Once);
        router.VerifyAll();
    }

    [Fact]
    public async Task OrchestrateAsync_WhenFormerInputPrefixWordIsSpoken_ReturnsTranscriptForApplyVerbatim()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
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
                return Task.FromResult<SpeechCaptureResult>(new SpeechCaptureResult("eingabe max mustermann", null));
            });

        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        var orchestrator = CreateOrchestrator(telemetry.Object, speechService.Object, router.Object);

        var contract = CreateMinimalContract();
        var interpreter = new FormInterpreterRuntimeOwner(
            contract,
            requestedIsoCode: "de",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));
        var context = CreateContext(contract);
        var field = await interpreter.GetFocusedFieldAsync();
        var session = new EvaluationSession("session-3", EvaluationChannel.Voice, "app", "app/current");
        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        stateTracker.BeginFieldPrompt(field.FieldGroupId);
        var autoHelp = CreateAutoHelpTracker(interpreter);

        var result = await orchestrator.OrchestrateAsync(
            context,
            interpreter,
            field,
            session,
            stateTracker,
            autoHelp,
            expectedInputMode: InputMode.Voice,
            CancellationToken.None);

        Assert.False(result.HasFinalResult);
        Assert.Equal("eingabe max mustermann", result.RequiredTranscriptForApply);
        Assert.NotNull(result.AmbiguityTelemetry);
        Assert.Equal(CommandSignalClass.NoCommand, result.AmbiguityTelemetry.Value.CommandSignalClass);
        Assert.Equal("context_default", result.AmbiguityTelemetry.Value.ResolutionStrategy);
        router.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task OrchestrateAsync_WhenEditValueCommandIsCaptured_UsesTheCorrectionPath()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
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
                return Task.FromResult<SpeechCaptureResult>(new SpeechCaptureResult("korrigieren", null));
            });
        speechService
            .Setup(s => s.SpeakSafeAsync(
                FormInputFeedbackPromptBuilder.GetCorrectionRequestedPrompt("de"),
                "de",
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        var orchestrator = CreateOrchestrator(telemetry.Object, speechService.Object, router.Object);

        var contract = CreateMinimalContract();
        var interpreter = new FormInterpreterRuntimeOwner(
            contract,
            requestedIsoCode: "de",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));
        var context = CreateContext(contract);
        var field = await interpreter.GetFocusedFieldAsync();
        var session = new EvaluationSession("session-edit", EvaluationChannel.Voice, "app", "app/current");
        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        var autoHelp = CreateAutoHelpTracker(interpreter);

        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.Prompting));
        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.Listening));
        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.Interpreting));
        telemetry.Setup(t => t.TrackRepairAttempt(session, field.FieldId, FormVoiceCommandFieldErrorCodes.ManualCorrection));
        telemetry.Setup(t => t.TrackStateEntry(session, DialogStates.Prompting));

        stateTracker.BeginFieldPrompt(field.FieldGroupId);

        var result = await orchestrator.OrchestrateAsync(
            context,
            interpreter,
            field,
            session,
            stateTracker,
            autoHelp,
            expectedInputMode: InputMode.Voice,
            CancellationToken.None);

        Assert.True(result.HasFinalResult);
        Assert.True(result.FinalResult!.ShouldContinue);
        Assert.Equal(DialogMicroStateIds.Prompting, stateTracker.CurrentMicroState);
        telemetry.VerifyAll();
        router.VerifyNoOtherCalls();
        speechService.VerifyAll();
    }

    [Fact]
    public async Task OrchestrateAsync_WhenRepeatIsEmbeddedInFlowingText_ClassifiesAsNoCommand()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
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
                return Task.FromResult<SpeechCaptureResult>(new SpeechCaptureResult("meine adresse ist wiederholen strasse 1", null));
            });

        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        var orchestrator = CreateOrchestrator(telemetry.Object, speechService.Object, router.Object);

        var contract = CreateMinimalContract();
        var interpreter = new FormInterpreterRuntimeOwner(
            contract,
            requestedIsoCode: "de",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));
        var context = CreateContext(contract);
        var field = await interpreter.GetFocusedFieldAsync();
        var session = new EvaluationSession("session-4", EvaluationChannel.Voice, "app", "app/current");
        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        stateTracker.BeginFieldPrompt(field.FieldGroupId);
        var autoHelp = CreateAutoHelpTracker(interpreter);

        var result = await orchestrator.OrchestrateAsync(
            context,
            interpreter,
            field,
            session,
            stateTracker,
            autoHelp,
            expectedInputMode: InputMode.Voice,
            CancellationToken.None);

        Assert.False(result.HasFinalResult);
        Assert.Equal("meine adresse ist wiederholen strasse 1", result.RequiredTranscriptForApply);
        Assert.NotNull(result.AmbiguityTelemetry);
        Assert.Equal(CommandSignalClass.NoCommand, result.AmbiguityTelemetry.Value.CommandSignalClass);
        Assert.Equal("context_default", result.AmbiguityTelemetry.Value.ResolutionStrategy);
        Assert.Null(result.AmbiguityTelemetry.Value.CommandId);

        router.VerifyNoOtherCalls();
    }

    // Im wörtlichen Modus wird „weiter“ zur Verarbeitung als Feldwert zurückgegeben, bevor der Befehlszweig aufgerufen wird.
    [Fact]
    public async Task OrchestrateAsync_WhenLiteralModeAndParsedNonOffCommand_ReturnsTranscriptForApplyWithoutExecuting()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
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
                return Task.FromResult<SpeechCaptureResult>(new SpeechCaptureResult("weiter", null));
            });

        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        var orchestrator = CreateOrchestrator(telemetry.Object, speechService.Object, router.Object);

        var contract = CreateMinimalContract();
        var interpreter = new FormInterpreterRuntimeOwner(
            contract,
            requestedIsoCode: "de",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));
        var context = CreateContext(contract);
        var field = await interpreter.GetFocusedFieldAsync();
        var session = new EvaluationSession("session-literal-capture", EvaluationChannel.Voice, "app", "app/current");
        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        stateTracker.BeginFieldPrompt(field.FieldGroupId);
        var autoHelp = CreateAutoHelpTracker(interpreter);

        var result = await orchestrator.OrchestrateAsync(
            context,
            interpreter,
            field,
            session,
            stateTracker,
            autoHelp,
            expectedInputMode: InputMode.Literal,
            CancellationToken.None);

        Assert.False(result.HasFinalResult);
        Assert.Equal("weiter", result.RequiredTranscriptForApply);
        Assert.Null(result.AmbiguityTelemetry);

        router.VerifyNoOtherCalls();
        telemetry.Verify(t => t.TrackGlobalCommand(It.IsAny<EvaluationSession>(), It.IsAny<string>()), Times.Never);
    }

    // Im wörtlichen Modus bleibt auch „hilfe“ ein Feldwert; der Befehlsrouter wird nicht aufgerufen.
    [Fact]
    public async Task OrchestrateAsync_WhenLiteralModeAndHelpUtterance_CapturesAsValueWithoutSpeakingHelp()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
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
                return Task.FromResult<SpeechCaptureResult>(new SpeechCaptureResult("hilfe", null));
            });

        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        var orchestrator = CreateOrchestrator(telemetry.Object, speechService.Object, router.Object);

        var contract = CreateMinimalContract();
        var interpreter = new FormInterpreterRuntimeOwner(
            contract,
            requestedIsoCode: "de",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));
        var context = CreateContext(contract);
        var field = await interpreter.GetFocusedFieldAsync();
        var session = new EvaluationSession("session-literal-help", EvaluationChannel.Voice, "app", "app/current");
        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        stateTracker.BeginFieldPrompt(field.FieldGroupId);
        var autoHelp = CreateAutoHelpTracker(interpreter);

        var result = await orchestrator.OrchestrateAsync(
            context,
            interpreter,
            field,
            session,
            stateTracker,
            autoHelp,
            expectedInputMode: InputMode.Literal,
            CancellationToken.None);

        Assert.False(result.HasFinalResult);
        Assert.Equal("hilfe", result.RequiredTranscriptForApply);
        Assert.Null(result.AmbiguityTelemetry);
        router.VerifyNoOtherCalls();
        telemetry.Verify(t => t.TrackGlobalCommand(It.IsAny<EvaluationSession>(), It.IsAny<string>()), Times.Never);
    }

    // Die drei Formen zum Beenden des wörtlichen Modus werden als InputModeLiteralOff an den Router weitergegeben.
    [Theory]
    [InlineData("wörtlich aus")]
    [InlineData("literal aus")]
    [InlineData("befehl wörtlich aus")]
    public async Task OrchestrateAsync_WhenLiteralModeAndLiteralOffCommand_ExecutesModeSwitch(
        string literalOffUtterance)
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
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
                return Task.FromResult<SpeechCaptureResult>(new SpeechCaptureResult(literalOffUtterance, null));
            });

        var expectedDialogResult = VoiceCommandDialogResult.BackToFormSelection(
            "app",
            "current",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));
        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        router
            .Setup(r => r.TryHandleAsync(
                CommandId.From(GlobalCommandIds.InputModeLiteralOff),
                It.IsAny<FormVoiceCommandGlobalCommandContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDialogResult);

        var orchestrator = CreateOrchestrator(telemetry.Object, speechService.Object, router.Object);

        var contract = CreateMinimalContract();
        var interpreter = new FormInterpreterRuntimeOwner(
            contract,
            requestedIsoCode: "de",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));
        var context = CreateContext(contract);
        var field = await interpreter.GetFocusedFieldAsync();
        var session = new EvaluationSession("session-literal-off", EvaluationChannel.Voice, "app", "app/current");
        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        stateTracker.BeginFieldPrompt(field.FieldGroupId);
        var autoHelp = CreateAutoHelpTracker(interpreter);

        var result = await orchestrator.OrchestrateAsync(
            context,
            interpreter,
            field,
            session,
            stateTracker,
            autoHelp,
            expectedInputMode: InputMode.Literal,
            CancellationToken.None);

        Assert.True(result.HasFinalResult);
        Assert.Same(expectedDialogResult, result.FinalResult!.DialogResult);
        router.VerifyAll();
    }

    // Im Tastaturmodus bleibt die Aufnahme von Feldwerten gesperrt. Stattdessen wird eine einzelne Befehlsaufnahme mit dem Ablehnungshinweis angefordert.
    [Fact]
    public async Task OrchestrateAsync_WhenKeyboardModeActive_CompletesWithoutInvokingCapture()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
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
                return Task.FromResult<SpeechCaptureResult>(new SpeechCaptureResult("Max", null));
            });

        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        var orchestrator = CreateOrchestrator(telemetry.Object, speechService.Object, router.Object);

        var contract = CreateMinimalContract();
        var interpreter = new FormInterpreterRuntimeOwner(
            contract,
            requestedIsoCode: "de",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));
        var context = CreateContext(contract);
        var field = await interpreter.GetFocusedFieldAsync();
        var session = new EvaluationSession("session-keyboard-precapture", EvaluationChannel.Voice, "app", "app/current");
        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        stateTracker.BeginFieldPrompt(field.FieldGroupId);
        var autoHelp = CreateAutoHelpTracker(interpreter);

        var result = await orchestrator.OrchestrateAsync(
            context,
            interpreter,
            field,
            session,
            stateTracker,
            autoHelp,
            expectedInputMode: InputMode.Keyboard,
            CancellationToken.None);

        Assert.True(result.HasFinalResult);
        speechService.Verify(
            s => s.CaptureTranscriptAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        speechService.Verify(
            s => s.CaptureConfirmationTranscriptAsync(
                FormInputFeedbackPromptBuilder.GetVoiceInputForbiddenPrompt(
                    contract,
                    "de",
                    PolitenessLevel.Formal,
                    silentInputFallback: null),
                "de",
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // Bei gesperrter Spracheingabe wird für das einzige Feld der Ablehnungshinweis angefordert.
    // Eine anschließende Befehlsaufnahme hält die Navigation erreichbar, ohne einen Feldwert aufzunehmen.
    [Fact]
    public async Task OrchestrateAsync_WhenVoiceInputForbiddenByPrivacyPolicy_CompletesWithoutInvokingCapture()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(s => s.SpeakSafeAsync(
                It.IsAny<string>(),
                "de",
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
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
                return Task.FromResult<SpeechCaptureResult>(new SpeechCaptureResult("Max", null));
            });

        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        var orchestrator = CreateOrchestrator(telemetry.Object, speechService.Object, router.Object);

        var contract = CreateMinimalContract(
            privacyClass: PrivacyClass.Secret,
            sensitiveDataPolicy: new SensitiveDataPolicy
            {
                VoiceInputPolicy = SensitiveDataPolicyDefaults.VoiceInputForbidden,
                SilentInputFallback = SensitiveDataPolicyDefaults.SilentInputFallbackKeyboard
            });
        var interpreter = new FormInterpreterRuntimeOwner(
            contract,
            requestedIsoCode: "de",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));
        var context = CreateContext(contract);
        var field = await interpreter.GetFocusedFieldAsync();
        var session = new EvaluationSession("session-forbidden-precapture", EvaluationChannel.Voice, "app", "app/current");
        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        stateTracker.BeginFieldPrompt(field.FieldGroupId);
        var autoHelp = CreateAutoHelpTracker(interpreter);

        var result = await orchestrator.OrchestrateAsync(
            context,
            interpreter,
            field,
            session,
            stateTracker,
            autoHelp,
            expectedInputMode: InputMode.Voice,
            CancellationToken.None);

        Assert.True(result.HasFinalResult);
        speechService.Verify(
            s => s.CaptureTranscriptAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        speechService.Verify(
            s => s.SpeakSafeAsync(
                FormInputFeedbackPromptBuilder.GetVoiceInputForbiddenPrompt(
                    contract,
                    "de",
                    PolitenessLevel.Formal,
                    "die Tastatur"),
                "de",
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        // Die Befehlsaufnahme beginnt mit einem neutralen Hinweis, bevor überhaupt eine Antwort vorliegt.
        speechService.Verify(
            s => s.CaptureConfirmationTranscriptAsync(
                FormInputFeedbackPromptBuilder.GetCommandParkWaitingPrompt(
                    contract,
                    "de",
                    PolitenessLevel.Formal),
                "de",
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // Der Befehl zum Wechsel zur Spracheingabe wird auch nach der Tastatureingabe-Sperre an den Router weitergegeben.
    [Fact]
    public async Task OrchestrateAsync_WhenKeyboardModeActive_ParkTurnExecutesModeCommand()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
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

        var expectedDialogResult = VoiceCommandDialogResult.BackToFormSelection(
            "app",
            "current",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));
        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        router
            .Setup(r => r.TryHandleAsync(
                CommandId.From(GlobalCommandIds.InputModeVoice),
                It.IsAny<FormVoiceCommandGlobalCommandContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDialogResult);

        var orchestrator = CreateOrchestrator(telemetry.Object, speechService.Object, router.Object);

        var contract = CreateMinimalContract();
        var interpreter = new FormInterpreterRuntimeOwner(
            contract,
            requestedIsoCode: "de",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));
        var context = CreateContext(contract);
        var field = await interpreter.GetFocusedFieldAsync();
        var session = new EvaluationSession("session-keyboard-park-command", EvaluationChannel.Voice, "app", "app/current");
        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        stateTracker.BeginFieldPrompt(field.FieldGroupId);
        var autoHelp = CreateAutoHelpTracker(interpreter);

        var result = await orchestrator.OrchestrateAsync(
            context,
            interpreter,
            field,
            session,
            stateTracker,
            autoHelp,
            expectedInputMode: InputMode.Keyboard,
            CancellationToken.None);

        Assert.True(result.HasFinalResult);
        Assert.Same(expectedDialogResult, result.FinalResult!.DialogResult);
        router.VerifyAll();
        speechService.Verify(
            s => s.CaptureTranscriptAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<RecognitionProfile>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // Eine ausbleibende Antwort auf die Befehlsaufnahme beendet diesen Dialogschritt ohne weitere Rückfrage.
    [Fact]
    public async Task OrchestrateAsync_WhenKeyboardModeActiveAndParkTurnSilent_AdvancesFocusWithoutReasking()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
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
            .ThrowsAsync(new NoSpeechDetectedException());

        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        var orchestrator = CreateOrchestrator(telemetry.Object, speechService.Object, router.Object);

        var contract = CreateMinimalContract();
        var interpreter = new FormInterpreterRuntimeOwner(
            contract,
            requestedIsoCode: "de",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));
        var context = CreateContext(contract);
        var field = await interpreter.GetFocusedFieldAsync();
        var session = new EvaluationSession("session-keyboard-park-silence", EvaluationChannel.Voice, "app", "app/current");
        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        stateTracker.BeginFieldPrompt(field.FieldGroupId);
        var autoHelp = CreateAutoHelpTracker(interpreter);

        var result = await orchestrator.OrchestrateAsync(
            context,
            interpreter,
            field,
            session,
            stateTracker,
            autoHelp,
            expectedInputMode: InputMode.Keyboard,
            CancellationToken.None);

        // Der Vertrag enthält kein Folgefeld; geprüft wird die Fortsetzung nach genau einer Befehlsaufnahme.
        Assert.True(result.HasFinalResult);
        Assert.True(result.FinalResult!.ShouldContinue);
        router.VerifyNoOtherCalls();
        speechService.Verify(
            s => s.CaptureConfirmationTranscriptAsync(
                It.IsAny<string>(),
                "de",
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // Eine Hilfsanfrage mit Aktivierungswort wird während der Bestätigungsfrage als neuer Befehl behandelt.
    // Die ursprüngliche Äußerung erhält keinen Annahmeeintrag.
    [Fact]
    public async Task OrchestrateAsync_WhenAmbiguousCommandQuestionAnsweredWithAnchoredHelp_ExecutesTheCommand()
    {
        var (result, telemetry, router, confirmationPrompts, stateTracker, helpPromptService) = await RunAmbiguousCommandTurnAsync(
            "befehl hilfe",
            router =>
            {
                router
                    .Setup(r => r.TryHandleAsync(
                        CommandId.From(GlobalCommandIds.Help),
                        It.IsAny<FormVoiceCommandGlobalCommandContext>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync((VoiceCommandDialogResult?)null);
            });

        Assert.True(result.HasFinalResult);
        Assert.True(result.FinalResult!.ShouldContinue);
        router.Verify(
            r => r.TryHandleAsync(
                CommandId.From(GlobalCommandIds.Help),
                It.IsAny<FormVoiceCommandGlobalCommandContext>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        telemetry.Verify(
            t => t.TrackGlobalCommand(It.IsAny<EvaluationSession>(), GlobalCommandIds.Help),
            Times.Once);
        telemetry.Verify(
            t => t.TrackCommandRecognitionFailure(
                It.IsAny<EvaluationSession>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);
        telemetry.Verify(
            t => t.TrackAmbiguityResolved(
                It.IsAny<EvaluationSession>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                "accepted",
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);
        // Die gespeicherten Texte enthalten die Frage nach zwei Optionen genau einmal.
        Assert.Equal(
            1,
            confirmationPrompts.Count(prompt => prompt.Contains("zwei Optionen", StringComparison.Ordinal)));
        // Der Hilfebefehl fordert weder automatische Hilfe noch eine erneute Strukturerklärung an.
        helpPromptService.VerifyNoOtherCalls();
        Assert.False(stateTracker.ConsumeStructuralTeachingReteach("name"));
    }

    [Fact]
    public async Task OrchestrateAsync_WhenAmbiguousCommandQuestionAnsweredWithAnchoredCancel_ExecutesTheCommand()
    {
        var expectedDialogResult = VoiceCommandDialogResult.BackToFormSelection(
            "app",
            "current",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));
        var (result, telemetry, router, _, _, _) = await RunAmbiguousCommandTurnAsync(
            "befehl abbrechen",
            router =>
            {
                router
                    .Setup(r => r.TryHandleAsync(
                        CommandId.From(GlobalCommandIds.Cancel),
                        It.IsAny<FormVoiceCommandGlobalCommandContext>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(expectedDialogResult);
            });

        Assert.True(result.HasFinalResult);
        Assert.Same(expectedDialogResult, result.FinalResult!.DialogResult);
        router.Verify(
            r => r.TryHandleAsync(
                CommandId.From(GlobalCommandIds.Cancel),
                It.IsAny<FormVoiceCommandGlobalCommandContext>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        telemetry.Verify(
            t => t.TrackCommandRecognitionFailure(
                It.IsAny<EvaluationSession>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);
        telemetry.Verify(
            t => t.TrackAmbiguityResolved(
                It.IsAny<EvaluationSession>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                "accepted",
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);
    }

    // Die Antwort „ja“ bestätigt den ursprünglichen Weiter-Befehl samt Annahmeeintrag.
    [Fact]
    public async Task OrchestrateAsync_WhenAmbiguousCommandQuestionAnsweredWithContinuation_ExecutesTheOriginalCommand()
    {
        var (result, telemetry, router, _, _, _) = await RunAmbiguousCommandTurnAsync(
            "ja",
            router =>
            {
                router
                    .Setup(r => r.TryHandleAsync(
                        CommandId.From(GlobalCommandIds.Next),
                        It.IsAny<FormVoiceCommandGlobalCommandContext>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync((VoiceCommandDialogResult?)null);
            });

        Assert.True(result.HasFinalResult);
        Assert.True(result.FinalResult!.ShouldContinue);
        router.Verify(
            r => r.TryHandleAsync(
                CommandId.From(GlobalCommandIds.Next),
                It.IsAny<FormVoiceCommandGlobalCommandContext>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        telemetry.Verify(
            t => t.TrackAmbiguityResolved(
                It.IsAny<EvaluationSession>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                "accepted",
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Once);
    }

    // Ohne Aktivierungswort wird „hilfe“ in dieser Bestätigungsfrage nicht ausgeführt und als Erkennungsfehler erfasst.
    [Fact]
    public async Task OrchestrateAsync_WhenAmbiguousCommandQuestionAnsweredWithBareHelp_StaysOnTheFailurePath()
    {
        var (result, telemetry, router, _, _, _) = await RunAmbiguousCommandTurnAsync(
            "hilfe",
            _ => { });

        Assert.True(result.HasFinalResult);
        Assert.True(result.FinalResult!.ShouldContinue);
        router.VerifyNoOtherCalls();
        telemetry.Verify(
            t => t.TrackGlobalCommand(It.IsAny<EvaluationSession>(), It.IsAny<string>()),
            Times.Never);
        telemetry.Verify(
            t => t.TrackCommandRecognitionFailure(
                It.IsAny<EvaluationSession>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Once);
    }

    private static async Task<(
        FormVoiceCommandFieldOrchestrationResult Result,
        Mock<IEvaluationTelemetry> Telemetry,
        Mock<IFormVoiceCommandGlobalCommandRouter> Router,
        List<string> ConfirmationPrompts,
        FormVoiceCommandStateTracker StateTracker,
        Mock<IFormVoiceCommandPromptService> HelpPromptService)> RunAmbiguousCommandTurnAsync(
        string answer,
        Action<Mock<IFormVoiceCommandGlobalCommandRouter>> setupRouter)
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var confirmationPrompts = new List<string>();
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(s => s.SpeakSafeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
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
                // Der Befehl mit Aktivierungswort liegt mit 70 Prozent Konfidenz im Bereich für eine Bestätigungsfrage.
                return Task.FromResult(new SpeechCaptureResult("befehl weiter", 70));
            });
        speechService
            .Setup(s => s.CaptureConfirmationTranscriptAsync(
                It.IsAny<string>(),
                "de",
                It.IsAny<FormVoiceCommandCaptureSettings>(),
                It.IsAny<Action>(),
                It.IsAny<SpeechCaptureTelemetryContext?>(),
                It.IsAny<SpeechKeyterms?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, string, FormVoiceCommandCaptureSettings, Action, SpeechCaptureTelemetryContext?, SpeechKeyterms?, CancellationToken>((prompt, _, _, onStartListening, _, _, _) =>
            {
                confirmationPrompts.Add(prompt);
                onStartListening();
                return Task.FromResult(new SpeechCaptureResult(answer, 90));
            });

        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        setupRouter(router);
        var orchestrator = CreateOrchestrator(telemetry.Object, speechService.Object, router.Object);

        var contract = CreateMinimalContract();
        var interpreter = new FormInterpreterRuntimeOwner(
            contract,
            requestedIsoCode: "de",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));
        var context = CreateContext(contract);
        var field = await interpreter.GetFocusedFieldAsync();
        var session = new EvaluationSession("session-ambiguous-command", EvaluationChannel.Voice, "app", "app/current");
        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        stateTracker.BeginFieldPrompt(field.FieldGroupId);
        var helpPromptService = new Mock<IFormVoiceCommandPromptService>(MockBehavior.Strict);
        var autoHelp = new FormVoiceCommandAutoHelpTracker(
            new VoiceDialogSettings { MaxRetries = 0 },
            helpPromptService.Object,
            new Mock<IFormVoiceCommandDialogStateTracker>(MockBehavior.Loose).Object,
            interpreter,
            "de");

        var result = await orchestrator.OrchestrateAsync(
            context,
            interpreter,
            field,
            session,
            stateTracker,
            autoHelp,
            expectedInputMode: InputMode.Voice,
            CancellationToken.None);

        return (result, telemetry, router, confirmationPrompts, stateTracker, helpPromptService);
    }

    private static DateComponentEntryWorkflowDriver CreateDateComponentEntryWorkflowDriver(
        IFormVoiceCommandSpeechService speechService)
    {
        var parser = new FormTranscriptParser(new YesNoTranscriptClassifier(), new ModeledNegationParticleResolver());
        return new DateComponentEntryWorkflowDriver(speechService, parser, parser);
    }

    private static FormVoiceCommandFieldTranscriptOrchestrator CreateOrchestrator(
        IEvaluationTelemetry telemetry,
        IFormVoiceCommandSpeechService speechService,
        IFormVoiceCommandGlobalCommandRouter router)
    {
        var commandExecutionService = new FormVoiceCommandGlobalCommandExecutionService(telemetry, router);
        var replayBuffer = new FormVoiceCommandSpokenOutputReplayBuffer();
        var errorHandlingService = new FormVoiceCommandFieldErrorHandlingService(telemetry, speechService, replayBuffer, new CaptureReplayPolicy());
        var digitsWorkflowService = CreateDigitsWorkflowService(telemetry, speechService);
        var ambiguityTelemetry = new FormVoiceCommandInputVsCommandTelemetry(
            telemetry,
            new InputVsCommandResolutionStrategyMapper());
        return new FormVoiceCommandFieldTranscriptOrchestrator(
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
                        new FormTranscriptParser(new YesNoTranscriptClassifier(), new ModeledNegationParticleResolver())),
                    new FormVoiceCommandConfirmationMissingDecisionPolicy()),
                telemetry),
            new FormVoiceCommandFieldEntryDependencies(
                new FormVoiceCommandInputAvailabilityGuard(errorHandlingService),
                new FormVoiceCommandConfirmationTranscriptCapture(speechService),
                new FormVoiceCommandReadOnlyFieldSpeaker(speechService, replayBuffer)),
            new FormVoiceCommandDateComponentTranscriptBranch(
                CreateDateComponentEntryWorkflowDriver(speechService)),
            new FormVoiceCommandDigitsTranscriptBranch(digitsWorkflowService),
            new FormVoiceCommandSpellingTranscriptBranch(CreateSpellingWorkflowService(telemetry, speechService)),
            new FormVoiceCommandSetTranscriptBranch(
                new FormVoiceCommandSetWorkflowService(
                    new CharacterAccumulationWorkflowDriver(NullLogger<CharacterAccumulationWorkflowDriver>.Instance),
                    AnchoredCommandFlowTestFactory.Create(),
                    new SetWorkflowCommandResolver(new SetWorkflowGlobalCommandMapper()),
                    new SetWorkflowCommandActionFlow(telemetry),
                    new SetWorkflowInputRejectionFlow(new WorkflowUnrecognizedInputAnnouncer(telemetry, speechService)),
                    new FormTranscriptParser(new YesNoTranscriptClassifier(), new ModeledNegationParticleResolver()),
                    speechService)),
            new FormVoiceCommandAmbiguityTranscriptBranch(
                new FormVoiceCommandAmbiguityResolver(),
                errorHandlingService,
                ambiguityTelemetry));
    }

    private static FormVoiceCommandSpellingWorkflowService CreateSpellingWorkflowService(
        IEvaluationTelemetry telemetry,
        IFormVoiceCommandSpeechService speechService)
        => new(
            new CharacterAccumulationWorkflowDriver(NullLogger<CharacterAccumulationWorkflowDriver>.Instance),
            AnchoredCommandFlowTestFactory.Create(),
            new SpellingWorkflowCommandResolver(
                new SpellingWorkflowLocalCommandMapper(),
                new SpellingWorkflowGlobalCommandMapper()),
            new FormTranscriptParser(new YesNoTranscriptClassifier(), new ModeledNegationParticleResolver()),
            speechService,
            new SpellingWorkflowCommandActionFlow(
                telemetry,
                speechService,
                new SpellingWorkflowNoValuePrompt(speechService)),
            new SpellingWorkflowInputRejectionFlow(
                new WorkflowUnrecognizedInputAnnouncer(telemetry, speechService)),
            new PhoneticSpellOutRenderer(EmbeddedFormTranscriptLexiconProvider.Instance));

    private static FormVoiceCommandDigitsWorkflowService CreateDigitsWorkflowService(
        IEvaluationTelemetry telemetry,
        IFormVoiceCommandSpeechService speechService)
    {
        var transcriptCapture = new DigitsWorkflowTranscriptCapture(speechService);
        return new FormVoiceCommandDigitsWorkflowService(
            new CharacterAccumulationWorkflowDriver(NullLogger<CharacterAccumulationWorkflowDriver>.Instance),
            AnchoredCommandFlowTestFactory.Create(),
            new FormTranscriptParser(new YesNoTranscriptClassifier(), new ModeledNegationParticleResolver()),
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

    private static FormContract CreateMinimalContract(
        PrivacyClass? privacyClass = null,
        SensitiveDataPolicy? sensitiveDataPolicy = null)
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
                    PrivacyClass = privacyClass,
                    SensitiveDataPolicy = sensitiveDataPolicy,
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
