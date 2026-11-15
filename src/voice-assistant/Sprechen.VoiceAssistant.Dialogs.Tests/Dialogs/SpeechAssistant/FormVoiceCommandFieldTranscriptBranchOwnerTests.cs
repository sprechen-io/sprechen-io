using Sprechen.Speech.TestDoubles;
using System.Collections.Immutable;
using System.Text.Json;
using Moq;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormVoiceCommandFieldTranscriptBranchOwnerTests
{
    [Fact]
    public async Task CaptureAsync_WhenNoSpeechDetected_ReturnsCompletedCaptureFailure()
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

        var owner = new FormVoiceCommandFieldTranscriptCapture(
            speechService.Object,
            new FormVoiceCommandFieldErrorHandlingService(telemetry.Object, speechService.Object, new FormVoiceCommandSpokenOutputReplayBuffer(), new CaptureReplayPolicy()));
        var turnContext = CreateTurnContext(telemetry.Object, CreateInterpreter(), readyForInterpretation: false);

        var result = await owner.CaptureAsync(turnContext, InputMode.Voice, CancellationToken.None);

        Assert.True(result.HasFinalResult);
        Assert.True(result.FinalResult!.HasFinalResult);
        Assert.True(result.FinalResult.FinalResult!.ShouldContinue);
        // Der Dialog meldet den Fehlercode, ohne zusätzlich TrackNoSpeechDetected aufzurufen.
        // So bleibt die Zuordnung zum Auswertungsereignis beim Telemetriedienst.
        telemetry.Verify(t => t.TrackNoSpeechDetected(
            It.IsAny<EvaluationSession>(),
            It.IsAny<string>()), Times.Never);
        telemetry.Verify(t => t.TrackConstraintViolation(
            turnContext.Session,
            turnContext.Field.FieldId,
            FormVoiceCommandFieldErrorCodes.NoSpeechDetected,
            SeverityLevels.Warning,
            TelemetryErrorTriggers.SystemRecognition), Times.Once);
        speechService.VerifyAll();
    }

    [Fact]
    public async Task CaptureAsync_WhenTheRecognizerReturnedNothing_DoesNotCountItAsAParticipantWhoSaidNothing()
    {
        // RecognizedAsEmpty kennzeichnet ein leeres Erkennungsergebnis und darf nicht als fehlende Eingabe eingeordnet werden.
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
            .ReturnsAsync(new SpeechCaptureResult(null, null) { RecognizedAsEmpty = true });

        var owner = new FormVoiceCommandFieldTranscriptCapture(
            speechService.Object,
            new FormVoiceCommandFieldErrorHandlingService(telemetry.Object, speechService.Object, new FormVoiceCommandSpokenOutputReplayBuffer(), new CaptureReplayPolicy()));
        var turnContext = CreateTurnContext(telemetry.Object, CreateInterpreter(), readyForInterpretation: false);

        await owner.CaptureAsync(turnContext, InputMode.Voice, CancellationToken.None);

        telemetry.Verify(
            t => t.TrackNoSpeechDetected(It.IsAny<EvaluationSession>(), It.IsAny<string>()),
            Times.Never);
        telemetry.Verify(t => t.TrackConstraintViolation(
            turnContext.Session,
            turnContext.Field.FieldId,
            FormVoiceCommandFieldErrorCodes.RecognizedAsEmpty,
            SeverityLevels.Warning,
            TelemetryErrorTriggers.SystemRecognition), Times.Once);
        speechService.VerifyAll();
    }

    [Fact]
    public async Task CaptureAsync_WhenNothingWasSpokenAtAll_IsStillRecordedAsNoInput()
    {
        // Ohne RecognizedAsEmpty wird eine leere Antwort weiterhin als NoInput erfasst.
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
            .ReturnsAsync(new SpeechCaptureResult(null, null));

        var owner = new FormVoiceCommandFieldTranscriptCapture(
            speechService.Object,
            new FormVoiceCommandFieldErrorHandlingService(telemetry.Object, speechService.Object, new FormVoiceCommandSpokenOutputReplayBuffer(), new CaptureReplayPolicy()));
        var turnContext = CreateTurnContext(telemetry.Object, CreateInterpreter(), readyForInterpretation: false);

        await owner.CaptureAsync(turnContext, InputMode.Voice, CancellationToken.None);

        telemetry.Verify(t => t.TrackConstraintViolation(
            turnContext.Session,
            turnContext.Field.FieldId,
            FormVoiceCommandFieldErrorCodes.NoInput,
            SeverityLevels.Warning,
            TelemetryErrorTriggers.SystemRecognition), Times.Once);
    }

    [Fact]
    public async Task CaptureAsync_WhenCaptureWasInterrupted_DoesNotCountItAsAParticipantWhoSaidNothing()
    {
        // Eine unterbrochene Aufnahme erhält ihren eigenen Fehlercode und darf keinen zusätzlichen Eintrag für ausbleibende Sprache auslösen.
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
            .ReturnsAsync(new SpeechCaptureResult(
                null,
                null,
                SpeechCaptureFailureCodes.CaptureInterrupted,
                true));

        var owner = new FormVoiceCommandFieldTranscriptCapture(
            speechService.Object,
            new FormVoiceCommandFieldErrorHandlingService(telemetry.Object, speechService.Object, new FormVoiceCommandSpokenOutputReplayBuffer(), new CaptureReplayPolicy()));
        var turnContext = CreateTurnContext(telemetry.Object, CreateInterpreter(), readyForInterpretation: false);

        await owner.CaptureAsync(turnContext, InputMode.Voice, CancellationToken.None);

        telemetry.Verify(
            t => t.TrackNoSpeechDetected(It.IsAny<EvaluationSession>(), It.IsAny<string>()),
            Times.Never);
        telemetry.Verify(t => t.TrackConstraintViolation(
            turnContext.Session,
            turnContext.Field.FieldId,
            FormVoiceCommandFieldErrorCodes.CaptureInterrupted,
            SeverityLevels.Warning,
            TelemetryErrorTriggers.SystemRecognition), Times.Once);
        speechService.VerifyAll();
    }

    [Fact]
    public async Task CaptureAsync_WhenSilencePersistsAcrossManyTurns_AlwaysStaysFieldLocalNeverAborts()
    {
        // Sieben aufeinanderfolgende NoSpeechDetectedException-Ergebnisse dürfen hier weder absenden noch den Dialog beenden.
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
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
            .ThrowsAsync(new NoSpeechDetectedException());

        var owner = new FormVoiceCommandFieldTranscriptCapture(
            speechService.Object,
            new FormVoiceCommandFieldErrorHandlingService(telemetry.Object, speechService.Object, new FormVoiceCommandSpokenOutputReplayBuffer(), new CaptureReplayPolicy()));
        var turnContext = CreateTurnContext(telemetry.Object, CreateInterpreter(), readyForInterpretation: false);

        for (var turn = 0; turn < 7; turn++)
        {
            var result = await owner.CaptureAsync(turnContext, InputMode.Voice, CancellationToken.None);

            Assert.True(result.HasFinalResult);
            var handling = result.FinalResult!.FinalResult!;
            Assert.True(handling.ShouldContinue);
            Assert.False(handling.WasSubmitted);
            Assert.Null(handling.DialogResult);
        }
    }

    // Ein als wörtliche Eingabe markierter „weiter“-Befehl bleibt in diesem Zweig unbehandelt.
    // Die Verarbeitung als Feldwert liegt beim aufrufenden Orchestrator.
    [Fact]
    public async Task TryHandleAsync_WhenLiteralModeNonOffCommand_DoesNotHandle()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService.ForwardConfirmationCaptureToValueCapture();
        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        var errorHandling = new FormVoiceCommandFieldErrorHandlingService(telemetry.Object, speechService.Object, new FormVoiceCommandSpokenOutputReplayBuffer(), new CaptureReplayPolicy());
        var owner = new ParsedCommandBranch(
            new FormVoiceCommandGlobalCommandExecutionService(telemetry.Object, router.Object),
            errorHandling,
            new FormVoiceCommandInputVsCommandTelemetry(
                telemetry.Object,
                new InputVsCommandResolutionStrategyMapper()),
            CreateConfirmationDecisionService(speechService.Object, telemetry.Object),
            telemetry.Object);
        var turnContext = CreateTurnContext(telemetry.Object, CreateInterpreter(), readyForInterpretation: true);

        var result = await owner.TryHandleAsync(
            turnContext,
            CreateInterpretation(
                inputTranscript: "weiter",
                hasParsedCommand: true,
                parsedCommandId: GlobalCommandIds.Next,
                commandSignalClass: CommandSignalClass.Clear,
                isLiteralMode: true),
            sttConfidencePercent: 95,
            CancellationToken.None);

        Assert.Null(result);
        router.VerifyNoOtherCalls();
        speechService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TryHandleAsync_WhenCommandConfidenceSufficient_ExecutesCommand()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService.ForwardConfirmationCaptureToValueCapture();
        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        router
            .Setup(r => r.TryHandleAsync(
                CommandId.From(GlobalCommandIds.Next),
                It.IsAny<FormVoiceCommandGlobalCommandContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((VoiceCommandDialogResult?)null);
        var owner = CreateBranch(telemetry.Object, speechService.Object, router.Object);
        var turnContext = CreateTurnContext(telemetry.Object, CreateInterpreter(), readyForInterpretation: true);

        var result = await owner.TryHandleAsync(
            turnContext,
            CreateExecutableCommandInterpretation(),
            sttConfidencePercent: 85,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result!.HasFinalResult);
        router.Verify(
            r => r.TryHandleAsync(
                CommandId.From(GlobalCommandIds.Next), It.IsAny<FormVoiceCommandGlobalCommandContext>(), It.IsAny<CancellationToken>()),
            Times.Once);
        telemetry.Verify(
            t => t.TrackCommandRecognitionFailure(
                It.IsAny<EvaluationSession>(),
                It.IsAny<string>(),
                It.IsAny<string?>()),
            Times.Never);
        speechService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TryHandleAsync_WhenCommandConfidenceUncertainAndUserConfirms_ExecutesCommand()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService.ForwardConfirmationCaptureToValueCapture();
        speechService
            .Setup(s => s.SpeakSafeAsync(It.IsAny<string>(), "de", It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
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
            .ReturnsAsync(new SpeechCaptureResult("ja", 99));
        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        router
            .Setup(r => r.TryHandleAsync(
                CommandId.From(GlobalCommandIds.Next),
                It.IsAny<FormVoiceCommandGlobalCommandContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((VoiceCommandDialogResult?)null);
        var yesNoParser = CreateYesNoParser(parsedValue: true);
        var owner = CreateBranch(telemetry.Object, speechService.Object, router.Object, yesNoParser);
        var turnContext = CreateTurnContext(telemetry.Object, CreateInterpreter(), readyForInterpretation: true);

        var result = await owner.TryHandleAsync(
            turnContext,
            CreateExecutableCommandInterpretation(),
            sttConfidencePercent: 70,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result!.HasFinalResult);
        router.Verify(
            r => r.TryHandleAsync(
                CommandId.From(GlobalCommandIds.Next), It.IsAny<FormVoiceCommandGlobalCommandContext>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TryHandleAsync_WhenCommandConfidenceUncertainAndUserDeclines_RejectsWithReprompt()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService.ForwardConfirmationCaptureToValueCapture();
        speechService
            .Setup(s => s.SpeakSafeAsync(It.IsAny<string>(), "de", It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
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
            .ReturnsAsync(new SpeechCaptureResult("nein", 99));
        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        var yesNoParser = CreateYesNoParser(parsedValue: false);
        var owner = CreateBranch(telemetry.Object, speechService.Object, router.Object, yesNoParser);
        var turnContext = CreateTurnContext(telemetry.Object, CreateInterpreter(), readyForInterpretation: true);

        var result = await owner.TryHandleAsync(
            turnContext,
            CreateExecutableCommandInterpretation(),
            sttConfidencePercent: 70,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result!.HasFinalResult);
        telemetry.Verify(
            t => t.TrackConstraintViolation(
                turnContext.Session,
                turnContext.Field.FieldId,
                FormVoiceCommandFieldErrorCodes.LowSttConfidence,
                SeverityLevels.Warning,
                TelemetryErrorTriggers.SystemRecognition),
            Times.Once);
        router.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TryHandleAsync_WhenCommandConfidenceInsufficient_RejectsWithoutExecuting()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService.ForwardConfirmationCaptureToValueCapture();
        speechService
            .Setup(s => s.SpeakSafeAsync(It.IsAny<string>(), "de", It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        var owner = CreateBranch(telemetry.Object, speechService.Object, router.Object);
        var turnContext = CreateTurnContext(telemetry.Object, CreateInterpreter(), readyForInterpretation: true);

        var result = await owner.TryHandleAsync(
            turnContext,
            CreateExecutableCommandInterpretation(),
            sttConfidencePercent: 40,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result!.HasFinalResult);
        telemetry.Verify(
            t => t.TrackConstraintViolation(
                turnContext.Session,
                turnContext.Field.FieldId,
                FormVoiceCommandFieldErrorCodes.LowSttConfidence,
                SeverityLevels.Warning,
                TelemetryErrorTriggers.SystemRecognition),
            Times.Once);
        telemetry.Verify(
            t => t.TrackCommandRecognitionFailure(
                turnContext.Session,
                FormVoiceCommandFieldErrorCodes.LowSttConfidence,
                null),
            Times.Once);
        router.VerifyNoOtherCalls();
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

    [Fact]
    public async Task TryHandleAsync_WhenCommandPrefixMatchesNoCommand_EmitsRecognitionFailure()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService.ForwardConfirmationCaptureToValueCapture();
        speechService
            .Setup(s => s.SpeakSafeAsync(It.IsAny<string>(), "de", It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        var owner = CreateBranch(telemetry.Object, speechService.Object, router.Object);
        var turnContext = CreateTurnContext(telemetry.Object, CreateInterpreter(), readyForInterpretation: true);

        var result = await owner.TryHandleAsync(
            turnContext,
            CreateUnknownCommandInterpretation(),
            sttConfidencePercent: 95,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result!.HasFinalResult);
        telemetry.Verify(
            t => t.TrackCommandRecognitionFailure(
                turnContext.Session,
                FormVoiceCommandFieldErrorCodes.UnknownCommand,
                null),
            Times.Once);
        router.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CaptureAsync_WhenTranscriptIsUsable_DoesNotEmitNoSpeech()
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
            .ReturnsAsync(new SpeechCaptureResult("max mustermann", 90));

        var owner = new FormVoiceCommandFieldTranscriptCapture(
            speechService.Object,
            new FormVoiceCommandFieldErrorHandlingService(telemetry.Object, speechService.Object, new FormVoiceCommandSpokenOutputReplayBuffer(), new CaptureReplayPolicy()));
        var turnContext = CreateTurnContext(telemetry.Object, CreateInterpreter(), readyForInterpretation: false);

        var result = await owner.CaptureAsync(turnContext, InputMode.Voice, CancellationToken.None);

        Assert.False(result.HasFinalResult);
        Assert.Equal("max mustermann", result.Transcript);
        telemetry.Verify(
            t => t.TrackNoSpeechDetected(It.IsAny<EvaluationSession>(), It.IsAny<string>()),
            Times.Never);
        speechService.VerifyAll();
    }

    [Fact]
    public async Task EvaluateAsync_WhenKeyboardModeIsActive_DescribesTheDenialOnce()
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
        var interpreter = CreateInterpreter(voiceInputAllowed: true);
        var owner = new FormVoiceCommandInputAvailabilityGuard(
            new FormVoiceCommandFieldErrorHandlingService(telemetry.Object, speechService.Object, new FormVoiceCommandSpokenOutputReplayBuffer(), new CaptureReplayPolicy()));
        var turnContext = CreateTurnContext(telemetry.Object, interpreter, readyForInterpretation: true);

        // Ablehnungsgrund ermitteln, anschließend Telemetrie und Ansage ausdrücklich auslösen.
        var denial = await owner.EvaluateAsync(turnContext, InputMode.Keyboard, CancellationToken.None);
        Assert.NotNull(denial);
        Assert.Equal(FormVoiceCommandFieldErrorCodes.KeyboardModeActive, denial!.IssueCode);
        Assert.Equal(InputModes.Keyboard, denial.ExpectedInputMode);
        Assert.False(string.IsNullOrWhiteSpace(denial.Prompt));

        await owner.TrackDenialAsync(turnContext, denial);
        await owner.AnnounceDenialAsync(turnContext, denial, CancellationToken.None);

        telemetry.Verify(t => t.TrackFieldAttempt(
            turnContext.Session,
            turnContext.Field.FieldId,
            InputModes.Keyboard,
            false,
            null), Times.Once);
        telemetry.Verify(t => t.TrackConstraintViolation(
            turnContext.Session,
            turnContext.Field.FieldId,
            FormVoiceCommandFieldErrorCodes.KeyboardModeActive,
            SeverityLevels.Warning,
            TelemetryErrorTriggers.UserInput), Times.Once);
        speechService.VerifyAll();
    }

    [Fact]
    public async Task ResolveAsync_WhenNoAmbiguity_ReturnsDefaultAcceptedTelemetry()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService.ForwardConfirmationCaptureToValueCapture();
        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        var errorHandling = new FormVoiceCommandFieldErrorHandlingService(telemetry.Object, speechService.Object, new FormVoiceCommandSpokenOutputReplayBuffer(), new CaptureReplayPolicy());
        var owner = new FormVoiceCommandAmbiguityTranscriptBranch(
            new FormVoiceCommandAmbiguityResolver(),
            errorHandling,
            new FormVoiceCommandInputVsCommandTelemetry(
                telemetry.Object,
                new InputVsCommandResolutionStrategyMapper()));
        var turnContext = CreateTurnContext(telemetry.Object, CreateInterpreter(), readyForInterpretation: true);

        var result = await owner.ResolveAsync(
            turnContext,
            CreateInterpretation(
                inputTranscript: "max mustermann",
                hasParsedCommand: false,
                parsedCommandId: string.Empty,
                commandSignalClass: CommandSignalClass.NoCommand,
                isLiteralMode: false),
            sttConfidencePercent: 87,
            CancellationToken.None);

        Assert.False(result.HasFinalResult);
        Assert.Equal("max mustermann", result.RequiredTranscriptForApply);
        Assert.Equal(87, result.SttConfidencePercent);
        Assert.NotNull(result.AmbiguityTelemetry);
        Assert.Equal("input_vs_command", result.AmbiguityTelemetry.Value.AmbiguityType);
        Assert.Equal("context_default", result.AmbiguityTelemetry.Value.ResolutionStrategy);
        Assert.Equal(CommandSignalClass.NoCommand, result.AmbiguityTelemetry.Value.CommandSignalClass);
        Assert.Equal("accepted", result.AmbiguityTelemetry.Value.Result);
        Assert.Null(result.AmbiguityTelemetry.Value.CommandId);
        router.VerifyNoOtherCalls();
        speechService.VerifyNoOtherCalls();
    }

    private static ParsedCommandBranch CreateBranch(
        IEvaluationTelemetry telemetry,
        IFormVoiceCommandSpeechService speechService,
        IFormVoiceCommandGlobalCommandRouter router,
        IFormTranscriptParser? yesNoParser = null)
    {
        var errorHandling = new FormVoiceCommandFieldErrorHandlingService(telemetry, speechService, new FormVoiceCommandSpokenOutputReplayBuffer(), new CaptureReplayPolicy());
        return new ParsedCommandBranch(
            new FormVoiceCommandGlobalCommandExecutionService(telemetry, router),
            errorHandling,
            new FormVoiceCommandInputVsCommandTelemetry(
                telemetry,
                new InputVsCommandResolutionStrategyMapper()),
            CreateConfirmationDecisionService(speechService, telemetry, yesNoParser),
            telemetry);
    }

    private static FormVoiceCommandConfirmationDecisionService CreateConfirmationDecisionService(
        IFormVoiceCommandSpeechService speechService,
        IEvaluationTelemetry telemetry,
        IFormTranscriptParser? yesNoParser = null)
    {
        _ = telemetry;
        return new FormVoiceCommandConfirmationDecisionService(
            new FormVoiceCommandConfirmationTranscriptCapture(speechService),
            new FormVoiceCommandConfirmationTranscriptInterpreter(
                new FormVoiceCommandConfirmationCommandDecisionMapper(),
                yesNoParser ?? CreateYesNoParser(parsedValue: false)),
            new FormVoiceCommandConfirmationMissingDecisionPolicy());
    }

    private static IFormTranscriptParser CreateYesNoParser(bool parsedValue)
    {
        var parser = new Mock<IFormTranscriptParser>(MockBehavior.Loose);
        parser
            .Setup(p => p.ParseYesNo(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(FormTranscriptParseOutcome.Succeeded(parsedValue ? "true" : "false"));
        return parser.Object;
    }

    private static FormVoiceCommandTranscriptInterpretation CreateExecutableCommandInterpretation()
    {
        return new FormVoiceCommandTranscriptInterpretation(
            new FormVoiceCommandTranscriptSource(
                TrimmedTranscript: "weiter",
                InputTranscript: "weiter",
                HasExplicitCommandPrefix: false),
            new FormVoiceCommandParsedCommand(
                HasParsedCommand: true,
                ParsedCommandId: GlobalCommandIds.Next,
                ParsedCommandArgument: null,
                CommandSignalClass: CommandSignalClass.Clear),
            new FormVoiceCommandInterpretationMode(
                IsTextField: true,
                IsLiteralMode: false,
                IsDigitsMode: false,
                ShouldExecuteParsedCommand: true,
                ShouldExecuteParsedCommandInDigitsMode: false));
    }

    private static FormVoiceCommandTranscriptInterpretation CreateUnknownCommandInterpretation()
    {
        return new FormVoiceCommandTranscriptInterpretation(
            new FormVoiceCommandTranscriptSource(
                TrimmedTranscript: "befehl wolkenkratzer",
                InputTranscript: "befehl wolkenkratzer",
                HasExplicitCommandPrefix: true),
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
    }

    private static FormVoiceCommandFieldTurnContext CreateTurnContext(
        IEvaluationTelemetry telemetry,
        IFormDialogRuntime interpreter,
        bool readyForInterpretation)
    {
        var contract = CreateMinimalContract();
        var context = CreateContext(contract);
        var field = CreateFieldSnapshot();
        var session = new EvaluationSession("session-branch-owner", EvaluationChannel.Voice, "app", "app/current");
        var stateTracker = new FormVoiceCommandStateTracker(telemetry, session);
        stateTracker.BeginFieldPrompt(field.FieldGroupId);
        if (readyForInterpretation)
        {
            stateTracker.BeginListening();
            stateTracker.BeginInterpretation();
        }

        var autoHelp = new FormVoiceCommandAutoHelpTracker(
            new VoiceDialogSettings { MaxRetries = 0 },
            Mock.Of<IFormVoiceCommandPromptService>(),
            stateTracker,
            interpreter,
            isoCode: "de");

        return new FormVoiceCommandFieldTurnContext(
            context,
            interpreter,
            field,
            session,
            stateTracker,
            autoHelp,
            new FormVoiceCommandUnrecognizedInputEscalationPolicy(),
            interpreter as IFormDialogPrivacyReadbackPolicy ?? Mock.Of<IFormDialogPrivacyReadbackPolicy>());
    }

    private static IFormDialogRuntime CreateInterpreter(bool voiceInputAllowed = true)
    {
        var interpreter = new Mock<IFormDialogRuntime>(MockBehavior.Strict);
        interpreter.As<IFormDialogPrivacyReadbackPolicy>()
            .Setup(i => i.IsVoiceInputAllowedAsync("name"))
            .ReturnsAsync(new VoiceInputAllowedResult(voiceInputAllowed, SensitiveDataPolicyDefaults.SilentInputFallbackAny));
        return interpreter.Object;
    }

    private static FormVoiceCommandTranscriptInterpretation CreateInterpretation(
        string inputTranscript,
        bool hasParsedCommand,
        string parsedCommandId,
        string commandSignalClass,
        bool isLiteralMode)
    {
        return new FormVoiceCommandTranscriptInterpretation(
            new FormVoiceCommandTranscriptSource(
                TrimmedTranscript: inputTranscript,
                InputTranscript: inputTranscript,
                HasExplicitCommandPrefix: false),
            new FormVoiceCommandParsedCommand(
                HasParsedCommand: hasParsedCommand,
                ParsedCommandId: parsedCommandId,
                ParsedCommandArgument: null,
                CommandSignalClass: commandSignalClass),
            new FormVoiceCommandInterpretationMode(
                IsTextField: true,
                IsLiteralMode: isLiteralMode,
                IsDigitsMode: false,
                ShouldExecuteParsedCommand: false,
                ShouldExecuteParsedCommandInDigitsMode: false));
    }

    private static FormFieldSnapshot CreateFieldSnapshot()
    {
        return new FormFieldSnapshot(
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
    }

    private static FormContract CreateMinimalContract()
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
