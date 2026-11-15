using Sprechen.Speech.TestDoubles;
using Moq;
using System.Collections.Immutable;
using System.Text.Json;
using Sprechen.Mda.Contracts.V1;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Mda.Contracts.V1.FormContracts;
using Sprechen.Mda.Contracts.V1.FormVoice;
using Sprechen.Mda.Contracts.V1.Identifiers;
using Sprechen.Mda.Contracts.V1.Telemetry;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class FormVoiceCommandFieldApplyServiceTests
{
    [Fact]
    public async Task ApplyAsync_WhenInputIsNotCaptured_ReturnsContinueAndSpeaksRecoveryPrompt()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(s => s.SpeakSafeAsync(It.IsAny<string>(), "de", It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        var submissionService = new Mock<IFormVoiceCommandSubmissionService>(MockBehavior.Strict);
        var service = CreateService(telemetry.Object, speechService.Object, router.Object, submissionService.Object);

        var contract = CreateMinimalContract();
        var context = CreateContext(contract);
        var field = CreateFieldSnapshot();
        var session = new EvaluationSession("session-1", EvaluationChannel.Voice, "app", "app/current");
        var interpreter = new Mock<IFormDialogRuntime>(MockBehavior.Strict);
        interpreter
            .Setup(i => i.TryApplyTranscriptAsync("ungueltig"))
            .ReturnsAsync(new FormInputResult(ValueCaptured: false, Violations: []));

        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        PrepareTrackerForApply(stateTracker, field.FieldGroupId);
        var autoHelp = CreateAutoHelpTracker(interpreter.Object);

        var result = await service.ApplyAsync(
            context,
            interpreter.Object,
            field,
            session,
            stateTracker,
            autoHelp,
            expectedInputMode: InputMode.Voice,
            transcriptForApply: "ungueltig",
            ambiguityTelemetry: null,
            sttConfidencePercent: null,
            CancellationToken.None);

        Assert.True(result.ShouldContinue);
        Assert.False(result.WasSubmitted);
        Assert.Null(result.DialogResult);
        telemetry.Verify(t => t.TrackFieldAttempt(session, field.FieldId, InputModes.Voice, false, null), Times.Once);
        speechService.VerifyAll();
        interpreter.VerifyAll();
        router.VerifyNoOtherCalls();
        submissionService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ApplyAsync_WhenConstraintViolationOccurs_ReturnsContinueAndSpeaksViolationPrompt()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(s => s.SpeakSafeAsync(It.IsAny<string>(), "de", It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        var submissionService = new Mock<IFormVoiceCommandSubmissionService>(MockBehavior.Strict);
        var service = CreateService(telemetry.Object, speechService.Object, router.Object, submissionService.Object);

        var contract = CreateMinimalContract();
        var context = CreateContext(contract);
        var field = CreateFieldSnapshot();
        var session = new EvaluationSession("session-2", EvaluationChannel.Voice, "app", "app/current");
        var violation = new FormConstraintViolation(
            FieldId: field.FieldId,
            ConstraintId: "name.required",
            Severity: SeverityLevels.Error,
            Code: "required",
            Message: "Name fehlt.",
            Remediation: "Bitte Namen nennen.",
            Scope: ConstraintScopes.Field,
            FieldGroupId: null);

        var interpreter = new Mock<IFormDialogRuntime>(MockBehavior.Strict);
        interpreter
            .Setup(i => i.TryApplyTranscriptAsync("max"))
            .ReturnsAsync(new FormInputResult(ValueCaptured: true, Violations: [violation]));
        interpreter
            .Setup(i => i.GetValuesSnapshotAsync())
            .ReturnsAsync(ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));
        interpreter.As<IFormDialogPrivacyReadbackPolicy>()
            .Setup(i => i.IsSpellOutAllowedInConfirmationAsync("name"))
            .ReturnsAsync(true);

        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        PrepareTrackerForApply(stateTracker, field.FieldGroupId);
        var autoHelp = CreateAutoHelpTracker(interpreter.Object);

        var result = await service.ApplyAsync(
            context,
            interpreter.Object,
            field,
            session,
            stateTracker,
            autoHelp,
            expectedInputMode: InputMode.Voice,
            transcriptForApply: "max",
            ambiguityTelemetry: null,
            sttConfidencePercent: null,
            CancellationToken.None);

        Assert.True(result.ShouldContinue);
        Assert.False(result.WasSubmitted);
        Assert.Null(result.DialogResult);
        telemetry.Verify(t => t.TrackFieldAttempt(session, field.FieldId, InputModes.Voice, true, null), Times.Once);
        telemetry.Verify(t => t.TrackConstraintViolation(
            session,
            field.FieldId,
            violation.Code,
            violation.Severity,
            violation.Scope,
            violation.FieldGroupId,
            TelemetryErrorTriggers.UserInput), Times.Once);
        speechService.VerifyAll();
        interpreter.VerifyAll();
        router.VerifyNoOtherCalls();
        submissionService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ApplyAsync_WhenInputIsValid_ContinuesWithConfirmationDecision()
    {
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
            .Returns<string, string, FormVoiceCommandCaptureSettings, Action, SpeechCaptureTelemetryContext?, RecognitionProfile, SpeechKeyterms?, CancellationToken>((_, _, _, onStartListening, _, _, _, _) =>
            {
                onStartListening();
                return Task.FromResult<SpeechCaptureResult>(new SpeechCaptureResult("ja", null));
            });
        // Der Mock erlaubt auch die kurze Bestätigung nach der Wertübernahme.
        speechService
            .Setup(s => s.SpeakSafeAsync(It.IsAny<string>(), "de", It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        var submissionService = new Mock<IFormVoiceCommandSubmissionService>(MockBehavior.Strict);
        var service = CreateService(telemetry.Object, speechService.Object, router.Object, submissionService.Object);

        var contract = CreateMinimalContract();
        var context = CreateContext(contract);
        var field = CreateFieldSnapshot();
        var session = new EvaluationSession("session-3", EvaluationChannel.Voice, "app", "app/current");
        var interpreter = new Mock<IFormDialogRuntime>(MockBehavior.Strict);
        interpreter
            .Setup(i => i.TryApplyTranscriptAsync("max mustermann"))
            .ReturnsAsync(new FormInputResult(ValueCaptured: true, Violations: []));
        interpreter
            .Setup(i => i.GetValuesSnapshotAsync())
            .ReturnsAsync(ImmutableDictionary.CreateRange(
                StringComparer.OrdinalIgnoreCase,
                [
                    new KeyValuePair<string, string>("name", "max mustermann")
                ]));
        interpreter.As<IFormDialogPrivacyReadbackPolicy>()
            .Setup(i => i.IsSpellOutAllowedInConfirmationAsync("name"))
            .ReturnsAsync(true);
        interpreter
            .Setup(i => i.TryMoveFocusNextAsync())
            .ReturnsAsync(true);
        interpreter
            .Setup(i => i.GetFocusedFieldAsync())
            .ReturnsAsync(field);

        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        PrepareTrackerForApply(stateTracker, field.FieldGroupId);
        var autoHelp = CreateAutoHelpTracker(interpreter.Object);

        var result = await service.ApplyAsync(
            context,
            interpreter.Object,
            field,
            session,
            stateTracker,
            autoHelp,
            expectedInputMode: InputMode.Voice,
            transcriptForApply: "max mustermann",
            ambiguityTelemetry: null,
            sttConfidencePercent: null,
            CancellationToken.None);

        Assert.True(result.ShouldContinue);
        Assert.False(result.WasSubmitted);
        Assert.Null(result.DialogResult);
        telemetry.Verify(t => t.TrackFieldAttempt(session, field.FieldId, InputModes.Voice, true, null), Times.Once);
        interpreter.Verify(i => i.TryMoveFocusNextAsync(), Times.Once);
        interpreter.VerifyAll();
        speechService.VerifyAll();
        router.VerifyNoOtherCalls();
        submissionService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ApplyAsync_AfterErrorPromptRelisten_AppliesCapturedValueWithoutThrowing()
    {
        // Dieselbe Folge wie nach einer unverstandenen Mengenoption: Fehleransage,
        // erneutes Zuhören, dann ausdrückliche Übernahme des schon gesammelten Werts.
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
            .Returns<string, string, FormVoiceCommandCaptureSettings, Action, SpeechCaptureTelemetryContext?, RecognitionProfile, SpeechKeyterms?, CancellationToken>((_, _, _, onStartListening, _, _, _, _) =>
            {
                onStartListening();
                return Task.FromResult<SpeechCaptureResult>(new SpeechCaptureResult("ja", null));
            });
        speechService
            .Setup(s => s.SpeakSafeAsync(It.IsAny<string>(), "de", It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        var submissionService = new Mock<IFormVoiceCommandSubmissionService>(MockBehavior.Strict);
        var service = CreateService(telemetry.Object, speechService.Object, router.Object, submissionService.Object);

        var contract = CreateMinimalContract();
        var context = CreateContext(contract);
        var field = CreateFieldSnapshot();
        var session = new EvaluationSession("session-error-relisten-apply", EvaluationChannel.Voice, "app", "app/current");
        var interpreter = new Mock<IFormDialogRuntime>(MockBehavior.Strict);
        interpreter
            .Setup(i => i.TryApplyTranscriptAsync("Musik, Weltraum"))
            .ReturnsAsync(new FormInputResult(ValueCaptured: true, Violations: []));
        interpreter
            .Setup(i => i.GetValuesSnapshotAsync())
            .ReturnsAsync(ImmutableDictionary.CreateRange(
                StringComparer.OrdinalIgnoreCase,
                [
                    new KeyValuePair<string, string>("name", "Musik, Weltraum")
                ]));
        interpreter.As<IFormDialogPrivacyReadbackPolicy>()
            .Setup(i => i.IsSpellOutAllowedInConfirmationAsync("name"))
            .ReturnsAsync(true);
        interpreter
            .Setup(i => i.TryMoveFocusNextAsync())
            .ReturnsAsync(true);
        interpreter
            .Setup(i => i.GetFocusedFieldAsync())
            .ReturnsAsync(field);

        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        PrepareTrackerForApply(stateTracker, field.FieldGroupId);
        stateTracker.BeginErrorPrompt();
        stateTracker.BeginListening();
        Assert.Equal(DialogMicroStateIds.Listening, stateTracker.CurrentMicroState);
        var autoHelp = CreateAutoHelpTracker(interpreter.Object);

        var result = await service.ApplyAsync(
            context,
            interpreter.Object,
            field,
            session,
            stateTracker,
            autoHelp,
            expectedInputMode: InputMode.Voice,
            transcriptForApply: "Musik, Weltraum",
            ambiguityTelemetry: null,
            sttConfidencePercent: 96,
            CancellationToken.None);

        Assert.True(result.ShouldContinue);
        Assert.False(result.WasSubmitted);
        Assert.Null(result.DialogResult);
        interpreter.Verify(i => i.TryApplyTranscriptAsync("Musik, Weltraum"), Times.Once);
        interpreter.Verify(i => i.TryMoveFocusNextAsync(), Times.Once);
    }

    [Fact]
    public async Task ApplyAsync_WhenSttConfidenceIsBelowConfirmThreshold_ReturnsContinueWithoutApplyingTranscript()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        var spokenPrompts = new List<string>();
        speechService
            .Setup(s => s.SpeakSafeAsync(It.IsAny<string>(), "de", It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, VoiceDialogSettings, CancellationToken>((text, _, _, _) => spokenPrompts.Add(text))
            .Returns(Task.CompletedTask);

        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        var submissionService = new Mock<IFormVoiceCommandSubmissionService>(MockBehavior.Strict);
        var service = CreateService(telemetry.Object, speechService.Object, router.Object, submissionService.Object);

        var contract = CreateMinimalContract();
        var context = CreateContext(contract);
        // Das Codefeld bleibt unterhalb der Bestätigungsschwelle gesperrt; die Ausnahme für Freitext gilt hier nicht.
        var field = CreateFieldSnapshot(codeStyle: true);
        var session = new EvaluationSession("session-4", EvaluationChannel.Voice, "app", "app/current");

        telemetry
            .Setup(t => t.TrackFieldAttempt(
                session,
                field.FieldId,
                InputModes.Voice,
                false,
                It.Is<int?>(value => value == 64)));
        telemetry
            .Setup(t => t.TrackConstraintViolation(
                session,
                field.FieldId,
                FormVoiceCommandFieldErrorCodes.LowSttConfidence,
                SeverityLevels.Warning,
                TelemetryErrorTriggers.SystemRecognition));
        telemetry
            .Setup(t => t.TrackRepairAttempt(
                session,
                field.FieldId,
                FormVoiceCommandFieldErrorCodes.LowSttConfidence));
        telemetry
            .Setup(t => t.TrackStateEntry(session, It.IsAny<string>()));

        var interpreter = new Mock<IFormDialogRuntime>(MockBehavior.Strict);
        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        PrepareTrackerForApply(stateTracker, field.FieldGroupId);
        var autoHelp = CreateAutoHelpTracker(interpreter.Object);

        var result = await service.ApplyAsync(
            context,
            interpreter.Object,
            field,
            session,
            stateTracker,
            autoHelp,
            expectedInputMode: InputMode.Voice,
            transcriptForApply: "max",
            ambiguityTelemetry: null,
            sttConfidencePercent: 64,
            CancellationToken.None);

        Assert.True(result.ShouldContinue);
        Assert.False(result.WasSubmitted);
        Assert.Null(result.DialogResult);
        // Die angeforderten Rückmeldungen dürfen das verworfene Transkript nicht enthalten.
        Assert.NotEmpty(spokenPrompts);
        Assert.DoesNotContain(spokenPrompts, prompt => prompt.Contains("max", StringComparison.OrdinalIgnoreCase));
        interpreter.VerifyNoOtherCalls();
        telemetry.VerifyAll();
        speechService.VerifyAll();
        router.VerifyNoOtherCalls();
        submissionService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ApplyAsync_WhenSpellingCaptureIsRejected_PlaysEscalationCueAndEntersReadbackRepair()
    {
        // Bei unzureichender Konfidenz im Buchstabiermodus wird das Signal für eine nötige Klärung angefordert.
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Loose);
        speechService.ForwardConfirmationCaptureToValueCapture();
        speechService
            .Setup(s => s.SpeakSafeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
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
            .ReturnsAsync(new SpeechCaptureResult(string.Empty, null));

        var earconPlayer = new Mock<IEarconPlayer>(MockBehavior.Loose);
        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        var submissionService = new Mock<IFormVoiceCommandSubmissionService>(MockBehavior.Strict);
        var service = CreateService(telemetry.Object, speechService.Object, router.Object, submissionService.Object, earconPlayer.Object);

        var contract = CreateMinimalContract();
        var context = CreateContext(contract);
        var field = CreateFieldSnapshot();
        var session = new EvaluationSession("session-spelling", EvaluationChannel.Voice, "app", "app/current");

        var interpreter = new Mock<IFormDialogRuntime>(MockBehavior.Loose);
        interpreter
            .Setup(i => i.GetValuesSnapshotAsync())
            .ReturnsAsync(ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));
        interpreter.As<IFormDialogPrivacyReadbackPolicy>()
            .Setup(i => i.IsSpellOutAllowedInConfirmationAsync("name"))
            .ReturnsAsync(false);

        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        PrepareTrackerForApply(stateTracker, field.FieldGroupId);
        var autoHelp = CreateAutoHelpTracker(interpreter.Object);

        var result = await service.ApplyAsync(
            context,
            interpreter.Object,
            field,
            session,
            stateTracker,
            autoHelp,
            expectedInputMode: InputMode.Spelling,
            transcriptForApply: "h a u s",
            ambiguityTelemetry: null,
            sttConfidencePercent: 40,
            CancellationToken.None);

        Assert.True(result.ShouldContinue);
        earconPlayer.Verify(e => e.PlayAsync(EarconId.ValidationAmbiguous, It.IsAny<CancellationToken>()), Times.Once);
    }

    // Ein wegen niedriger Konfidenz abgelehnter Wert darf in der Rückfrage nicht zur Übernahme angeboten werden.
    // Der Test prüft die Angebote zum Ändern und erneuten Buchstabieren im angeforderten Text.
    [Fact]
    public async Task ApplyAsync_WhenSpellingCaptureIsRejected_OffersOnlyExecutableRepairActions()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var capturePrompts = new List<string>();
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Loose);
        speechService.ForwardConfirmationCaptureToValueCapture();
        speechService
            .Setup(s => s.SpeakSafeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
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
                (prompt, _, _, _, _, _, _, _) =>
                {
                    capturePrompts.Add(prompt);
                    return Task.FromResult(new SpeechCaptureResult(string.Empty, null));
                });

        var earconPlayer = new Mock<IEarconPlayer>(MockBehavior.Loose);
        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        var submissionService = new Mock<IFormVoiceCommandSubmissionService>(MockBehavior.Strict);
        var service = CreateService(telemetry.Object, speechService.Object, router.Object, submissionService.Object, earconPlayer.Object);

        var contract = CreateMinimalContract();
        var context = CreateContext(contract);
        var field = CreateFieldSnapshot();
        var session = new EvaluationSession("session-spelling-offer", EvaluationChannel.Voice, "app", "app/current");

        var interpreter = new Mock<IFormDialogRuntime>(MockBehavior.Loose);
        interpreter
            .Setup(i => i.GetValuesSnapshotAsync())
            .ReturnsAsync(ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));
        interpreter.As<IFormDialogPrivacyReadbackPolicy>()
            .Setup(i => i.IsSpellOutAllowedInConfirmationAsync("name"))
            .ReturnsAsync(false);

        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        PrepareTrackerForApply(stateTracker, field.FieldGroupId);
        var autoHelp = CreateAutoHelpTracker(interpreter.Object);

        var result = await service.ApplyAsync(
            context,
            interpreter.Object,
            field,
            session,
            stateTracker,
            autoHelp,
            expectedInputMode: InputMode.Spelling,
            transcriptForApply: "h a u s",
            ambiguityTelemetry: null,
            sttConfidencePercent: 40,
            CancellationToken.None);

        Assert.True(result.ShouldContinue);
        Assert.NotEmpty(capturePrompts);
        Assert.Contains(
            capturePrompts,
            prompt => prompt.Contains("ändern", StringComparison.OrdinalIgnoreCase)
                && prompt.Contains("buchstabieren", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(
            capturePrompts,
            prompt => prompt.Contains("übernehmen", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ApplyAsync_WhenSpellingEscalationAnswerIsEdit_SpeaksCorrectionRequestedAndContinues()
    {
        var (result, spokenPrompts, interpreter) = await ApplyRejectedSpellingWithAnswerAsync("ändern");

        Assert.True(result.ShouldContinue);
        Assert.Contains(spokenPrompts, prompt => prompt.Contains("korrigierten Wert", StringComparison.Ordinal));
        interpreter.Verify(i => i.TryApplyTranscriptAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ApplyAsync_WhenSpellingEscalationAnswerIsSpellAgain_RepromptsWithoutCommitting()
    {
        var (result, spokenPrompts, interpreter) = await ApplyRejectedSpellingWithAnswerAsync("buchstabieren");

        Assert.True(result.ShouldContinue);
        Assert.Contains(spokenPrompts, prompt => prompt.Contains("korrigierten Wert", StringComparison.Ordinal));
        interpreter.Verify(i => i.TryApplyTranscriptAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ApplyAsync_WhenSpellingEscalationAnswerIsKeep_NeverCommitsTheRejectedValue()
    {
        var (result, _, interpreter) = await ApplyRejectedSpellingWithAnswerAsync("übernehmen");

        Assert.True(result.ShouldContinue);
        interpreter.Verify(i => i.TryApplyTranscriptAsync(It.IsAny<string>()), Times.Never);
    }

    // Die Antwort mit Aktivierungswort wird als globaler Befehl weitergeleitet und als solcher erfasst.
    [Fact]
    public async Task ApplyAsync_WhenSpellingEscalationAnswerIsAnchoredHelp_ExecutesTheCommand()
    {
        var (result, _, _, router, telemetry, stateTracker, helpPromptService) = await ApplyRejectedSpellingWithAnchoredCommandAsync(
            "befehl hilfe",
            GlobalCommandIds.Help,
            routerResult: null);

        Assert.True(result.ShouldContinue);
        router.Verify(
            r => r.TryHandleAsync(
                CommandId.From(GlobalCommandIds.Help),
                It.IsAny<FormVoiceCommandGlobalCommandContext>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        telemetry.Verify(
            t => t.TrackGlobalCommand(It.IsAny<EvaluationSession>(), CommandId.From(GlobalCommandIds.Help)),
            Times.Once);
        // Für den Hilfebefehl dürfen keine Reparatur- oder Fehlereinträge wegen niedriger Konfidenz entstehen.
        telemetry.Verify(
            t => t.TrackRepairAttempt(
                It.IsAny<EvaluationSession>(),
                "name",
                FormVoiceCommandFieldErrorCodes.LowSttConfidence),
            Times.Never);
        telemetry.Verify(
            t => t.TrackConstraintViolation(
                It.IsAny<EvaluationSession>(),
                "name",
                FormVoiceCommandFieldErrorCodes.LowSttConfidence,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<string>()),
            Times.Never);
        // Der Hilfebefehl fordert weder automatische Hilfe noch eine erneute Erklärung der Feldstruktur an.
        helpPromptService.VerifyNoOtherCalls();
        Assert.False(stateTracker.ConsumeStructuralTeachingReteach("name"));
    }

    [Fact]
    public async Task ApplyAsync_WhenSpellingEscalationAnswerIsAnchoredCancel_ExecutesTheCommand()
    {
        var expectedDialogResult = VoiceCommandDialogResult.BackToFormSelection(
            "app",
            "current",
            ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));
        var (result, _, _, router, telemetry, stateTracker, helpPromptService) = await ApplyRejectedSpellingWithAnchoredCommandAsync(
            "befehl abbrechen",
            GlobalCommandIds.Cancel,
            expectedDialogResult);

        Assert.Same(expectedDialogResult, result.DialogResult);
        router.Verify(
            r => r.TryHandleAsync(
                CommandId.From(GlobalCommandIds.Cancel),
                It.IsAny<FormVoiceCommandGlobalCommandContext>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        telemetry.Verify(
            t => t.TrackRepairAttempt(
                It.IsAny<EvaluationSession>(),
                "name",
                FormVoiceCommandFieldErrorCodes.LowSttConfidence),
            Times.Never);
        helpPromptService.VerifyNoOtherCalls();
        Assert.False(stateTracker.ConsumeStructuralTeachingReteach("name"));
    }

    // Ohne Aktivierungswort bleibt „hilfe“ in diesem Dialog ohne Befehlsausführung; die Antwort wird als unverstanden behandelt.
    [Fact]
    public async Task ApplyAsync_WhenSpellingEscalationAnswerIsBareHelp_StaysUnexecuted()
    {
        var (result, spokenPrompts, _, router, telemetry, _, _) = await ApplyRejectedSpellingWithAnchoredCommandAsync(
            "hilfe",
            expectedCommandId: null,
            routerResult: null);

        Assert.True(result.ShouldContinue);
        router.VerifyNoOtherCalls();
        telemetry.Verify(
            t => t.TrackGlobalCommand(It.IsAny<EvaluationSession>(), It.IsAny<string>()),
            Times.Never);
        telemetry.Verify(
            t => t.TrackRepairAttempt(
                It.IsAny<EvaluationSession>(),
                "name",
                FormVoiceCommandFieldErrorCodes.UnrecognizedInput),
            Times.Once);
        Assert.Contains(spokenPrompts, prompt => prompt.Contains("nicht verstanden", StringComparison.Ordinal));
    }

    private static async Task<(
        FormVoiceCommandFieldHandlingResult Result,
        List<string> SpokenPrompts,
        Mock<IFormDialogRuntime> Interpreter,
        Mock<IFormVoiceCommandGlobalCommandRouter> Router,
        Mock<IEvaluationTelemetry> Telemetry,
        FormVoiceCommandStateTracker StateTracker,
        Mock<IFormVoiceCommandPromptService> HelpPromptService)> ApplyRejectedSpellingWithAnchoredCommandAsync(
        string answer,
        string? expectedCommandId,
        VoiceCommandDialogResult? routerResult)
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var spokenPrompts = new List<string>();
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Loose);
        speechService.ForwardConfirmationCaptureToValueCapture();
        speechService
            .Setup(s => s.SpeakSafeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
            .Returns<string, string, VoiceDialogSettings, CancellationToken>((text, _, _, _) =>
            {
                spokenPrompts.Add(text);
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
            .ReturnsAsync(new SpeechCaptureResult(answer, 90));

        var earconPlayer = new Mock<IEarconPlayer>(MockBehavior.Loose);
        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        if (expectedCommandId is not null)
        {
            router
                .Setup(r => r.TryHandleAsync(
                    CommandId.From(expectedCommandId),
                    It.IsAny<FormVoiceCommandGlobalCommandContext>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(routerResult);
        }

        var submissionService = new Mock<IFormVoiceCommandSubmissionService>(MockBehavior.Strict);
        var service = CreateService(telemetry.Object, speechService.Object, router.Object, submissionService.Object, earconPlayer.Object);

        var contract = CreateMinimalContract();
        var context = CreateContext(contract);
        var field = CreateFieldSnapshot();
        var session = new EvaluationSession("session-spelling-command", EvaluationChannel.Voice, "app", "app/current");

        var interpreter = new Mock<IFormDialogRuntime>(MockBehavior.Loose);
        interpreter
            .Setup(i => i.GetValuesSnapshotAsync())
            .ReturnsAsync(ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));
        interpreter.As<IFormDialogPrivacyReadbackPolicy>()
            .Setup(i => i.IsSpellOutAllowedInConfirmationAsync("name"))
            .ReturnsAsync(false);

        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        PrepareTrackerForApply(stateTracker, field.FieldGroupId);
        var helpPromptService = new Mock<IFormVoiceCommandPromptService>(MockBehavior.Strict);
        var autoHelp = new FormVoiceCommandAutoHelpTracker(
            new VoiceDialogSettings { MaxRetries = 0 },
            helpPromptService.Object,
            new Mock<IFormVoiceCommandDialogStateTracker>(MockBehavior.Loose).Object,
            interpreter.Object,
            "de");

        var result = await service.ApplyAsync(
            context,
            interpreter.Object,
            field,
            session,
            stateTracker,
            autoHelp,
            expectedInputMode: InputMode.Spelling,
            transcriptForApply: "h a u s",
            ambiguityTelemetry: null,
            sttConfidencePercent: 40,
            CancellationToken.None);

        return (result, spokenPrompts, interpreter, router, telemetry, stateTracker, helpPromptService);
    }

    private static async Task<(
        FormVoiceCommandFieldHandlingResult Result,
        List<string> SpokenPrompts,
        Mock<IFormDialogRuntime> Interpreter)> ApplyRejectedSpellingWithAnswerAsync(string answer)
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var spokenPrompts = new List<string>();
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Loose);
        speechService.ForwardConfirmationCaptureToValueCapture();
        speechService
            .Setup(s => s.SpeakSafeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
            .Returns<string, string, VoiceDialogSettings, CancellationToken>((text, _, _, _) =>
            {
                spokenPrompts.Add(text);
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
            .ReturnsAsync(new SpeechCaptureResult(answer, 90));

        var earconPlayer = new Mock<IEarconPlayer>(MockBehavior.Loose);
        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        var submissionService = new Mock<IFormVoiceCommandSubmissionService>(MockBehavior.Strict);
        var service = CreateService(telemetry.Object, speechService.Object, router.Object, submissionService.Object, earconPlayer.Object);

        var contract = CreateMinimalContract();
        var context = CreateContext(contract);
        var field = CreateFieldSnapshot();
        var session = new EvaluationSession("session-spelling-answer", EvaluationChannel.Voice, "app", "app/current");

        var interpreter = new Mock<IFormDialogRuntime>(MockBehavior.Loose);
        interpreter
            .Setup(i => i.GetValuesSnapshotAsync())
            .ReturnsAsync(ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));
        // Der Mock liefert beim Wechsel des Eingabemodus false und bildet damit einen unveränderten Modus ab.
        interpreter
            .Setup(i => i.GetFocusedFieldAsync())
            .ReturnsAsync(field);
        interpreter
            .Setup(i => i.TrySetFocusedFieldInputModeAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        interpreter.As<IFormDialogPrivacyReadbackPolicy>()
            .Setup(i => i.IsSpellOutAllowedInConfirmationAsync("name"))
            .ReturnsAsync(false);

        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        PrepareTrackerForApply(stateTracker, field.FieldGroupId);
        var autoHelp = CreateAutoHelpTracker(interpreter.Object);

        var result = await service.ApplyAsync(
            context,
            interpreter.Object,
            field,
            session,
            stateTracker,
            autoHelp,
            expectedInputMode: InputMode.Spelling,
            transcriptForApply: "h a u s",
            ambiguityTelemetry: null,
            sttConfidencePercent: 40,
            CancellationToken.None);

        return (result, spokenPrompts, interpreter);
    }


    [Theory]
    [InlineData(InputMode.Voice)]
    [InlineData(InputMode.Digits)]
    public async Task ApplyAsync_WhenNonSpellingCaptureIsRejected_UsesUnchangedGatePathWithoutEscalationCue(InputMode mode)
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Loose);
        speechService.ForwardConfirmationCaptureToValueCapture();
        speechService
            .Setup(s => s.SpeakSafeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var earconPlayer = new Mock<IEarconPlayer>(MockBehavior.Loose);
        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        var submissionService = new Mock<IFormVoiceCommandSubmissionService>(MockBehavior.Strict);
        var service = CreateService(telemetry.Object, speechService.Object, router.Object, submissionService.Object, earconPlayer.Object);

        var contract = CreateMinimalContract();
        var context = CreateContext(contract);
        // Das Codefeld verhindert, dass die Freitextausnahme die Ablehnung wegen niedriger Konfidenz umgeht.
        var field = CreateFieldSnapshot(codeStyle: true);
        var session = new EvaluationSession("session-nonspelling", EvaluationChannel.Voice, "app", "app/current");
        var interpreter = new Mock<IFormDialogRuntime>(MockBehavior.Strict);
        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        PrepareTrackerForApply(stateTracker, field.FieldGroupId);
        var autoHelp = CreateAutoHelpTracker(interpreter.Object);

        var result = await service.ApplyAsync(
            context,
            interpreter.Object,
            field,
            session,
            stateTracker,
            autoHelp,
            expectedInputMode: mode,
            transcriptForApply: "h a u s",
            ambiguityTelemetry: null,
            sttConfidencePercent: 40,
            CancellationToken.None);

        Assert.True(result.ShouldContinue);
        earconPlayer.Verify(e => e.PlayAsync(It.IsAny<EarconId>(), It.IsAny<CancellationToken>()), Times.Never);
        interpreter.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ApplyAsync_WhenSttConfidenceEqualsConfirmThreshold_AppliesTranscriptAndTracksConfidencePercent()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(s => s.SpeakSafeAsync(It.IsAny<string>(), "de", It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        var submissionService = new Mock<IFormVoiceCommandSubmissionService>(MockBehavior.Strict);
        var service = CreateService(telemetry.Object, speechService.Object, router.Object, submissionService.Object);

        var contract = CreateMinimalContract();
        var context = CreateContext(contract);
        var field = CreateFieldSnapshot();
        var session = new EvaluationSession("session-5", EvaluationChannel.Voice, "app", "app/current");

        telemetry
            .Setup(t => t.TrackFieldAttempt(
                session,
                field.FieldId,
                InputModes.Voice,
                false,
                It.Is<int?>(value => value == 65)));
        telemetry
            .Setup(t => t.TrackConstraintViolation(
                session,
                field.FieldId,
                FormVoiceCommandFieldErrorCodes.UnrecognizedInput,
                SeverityLevels.Warning,
                TelemetryErrorTriggers.SystemRecognition));
        telemetry
            .Setup(t => t.TrackRepairAttempt(
                session,
                field.FieldId,
                FormVoiceCommandFieldErrorCodes.UnrecognizedInput));
        telemetry
            .Setup(t => t.TrackStateEntry(session, It.IsAny<string>()));

        var interpreter = new Mock<IFormDialogRuntime>(MockBehavior.Strict);
        interpreter
            .Setup(i => i.TryApplyTranscriptAsync("max"))
            .ReturnsAsync(new FormInputResult(ValueCaptured: false, Violations: []));
        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        PrepareTrackerForApply(stateTracker, field.FieldGroupId);
        var autoHelp = CreateAutoHelpTracker(interpreter.Object);

        var result = await service.ApplyAsync(
            context,
            interpreter.Object,
            field,
            session,
            stateTracker,
            autoHelp,
            expectedInputMode: InputMode.Voice,
            transcriptForApply: "max",
            ambiguityTelemetry: null,
            sttConfidencePercent: 65,
            CancellationToken.None);

        Assert.True(result.ShouldContinue);
        Assert.False(result.WasSubmitted);
        Assert.Null(result.DialogResult);
        interpreter.VerifyAll();
        telemetry.VerifyAll();
        speechService.VerifyAll();
        router.VerifyNoOtherCalls();
        submissionService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ApplyAsync_WhenContractOverridesSttThresholds_UsesContractThresholds()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(s => s.SpeakSafeAsync(It.IsAny<string>(), "de", It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        var submissionService = new Mock<IFormVoiceCommandSubmissionService>(MockBehavior.Strict);
        var service = CreateService(telemetry.Object, speechService.Object, router.Object, submissionService.Object);

        var contract = CreateMinimalContract(formVoiceProfile: new FormVoiceProfile
        {
            SttMinConfidenceToAccept = 95,
            SttMinConfidenceToConfirm = 75
        });
        var context = CreateContext(contract);
        // Am Codefeld gilt die im Vertrag gesetzte Bestätigungsschwelle ohne Freitextausnahme.
        var field = CreateFieldSnapshot(codeStyle: true);
        var session = new EvaluationSession("session-5b", EvaluationChannel.Voice, "app", "app/current");

        telemetry
            .Setup(t => t.TrackFieldAttempt(
                session,
                field.FieldId,
                InputModes.Voice,
                false,
                It.Is<int?>(value => value == 74)));
        telemetry
            .Setup(t => t.TrackConstraintViolation(
                session,
                field.FieldId,
                FormVoiceCommandFieldErrorCodes.LowSttConfidence,
                SeverityLevels.Warning,
                TelemetryErrorTriggers.SystemRecognition));
        telemetry
            .Setup(t => t.TrackRepairAttempt(
                session,
                field.FieldId,
                FormVoiceCommandFieldErrorCodes.LowSttConfidence));
        telemetry
            .Setup(t => t.TrackStateEntry(session, It.IsAny<string>()));

        var interpreter = new Mock<IFormDialogRuntime>(MockBehavior.Strict);
        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        PrepareTrackerForApply(stateTracker, field.FieldGroupId);
        var autoHelp = CreateAutoHelpTracker(interpreter.Object);

        var result = await service.ApplyAsync(
            context,
            interpreter.Object,
            field,
            session,
            stateTracker,
            autoHelp,
            expectedInputMode: InputMode.Voice,
            transcriptForApply: "max",
            ambiguityTelemetry: null,
            sttConfidencePercent: 74,
            CancellationToken.None);

        Assert.True(result.ShouldContinue);
        Assert.False(result.WasSubmitted);
        Assert.Null(result.DialogResult);
        interpreter.VerifyNoOtherCalls();
        telemetry.VerifyAll();
        speechService.VerifyAll();
        router.VerifyNoOtherCalls();
        submissionService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ApplyAsync_WhenSttConfidenceIsUncertain_AndConfirmationIsUndecided_DoesNotAdvanceFocus()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
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
        speechService
            .Setup(s => s.SpeakSafeAsync(It.IsAny<string>(), "de", It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        var submissionService = new Mock<IFormVoiceCommandSubmissionService>(MockBehavior.Strict);
        var service = CreateService(telemetry.Object, speechService.Object, router.Object, submissionService.Object);

        var contract = CreateMinimalContract();
        var context = CreateContext(contract);
        var field = CreateFieldSnapshot();
        var session = new EvaluationSession("session-6", EvaluationChannel.Voice, "app", "app/current");

        telemetry
            .Setup(t => t.TrackFieldAttempt(
                session,
                field.FieldId,
                InputModes.Voice,
                true,
                It.Is<int?>(value => value == 70)));
        telemetry
            .Setup(t => t.TrackRepairAttempt(
                session,
                field.FieldId,
                FormVoiceCommandFieldErrorCodes.UnrecognizedInput));
        // Der Mock erwartet auch einen Reparatureintrag mit NoInput für die ausbleibende Bestätigungsantwort.
        telemetry
            .Setup(t => t.TrackRepairAttempt(
                session,
                field.FieldId,
                FormVoiceCommandFieldErrorCodes.NoInput));
        telemetry
            .Setup(t => t.TrackStateEntry(session, It.IsAny<string>()));

        var interpreter = new Mock<IFormDialogRuntime>(MockBehavior.Strict);
        interpreter
            .Setup(i => i.TryApplyTranscriptAsync("max mustermann"))
            .ReturnsAsync(new FormInputResult(ValueCaptured: true, Violations: []));
        interpreter
            .Setup(i => i.GetValuesSnapshotAsync())
            .ReturnsAsync(ImmutableDictionary.CreateRange(
                StringComparer.OrdinalIgnoreCase,
                [
                    new KeyValuePair<string, string>("name", "max mustermann")
                ]));
        interpreter.As<IFormDialogPrivacyReadbackPolicy>()
            .Setup(i => i.IsSpellOutAllowedInConfirmationAsync("name"))
            .ReturnsAsync(true);

        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        PrepareTrackerForApply(stateTracker, field.FieldGroupId);
        var autoHelp = CreateAutoHelpTracker(interpreter.Object);

        var result = await service.ApplyAsync(
            context,
            interpreter.Object,
            field,
            session,
            stateTracker,
            autoHelp,
            expectedInputMode: InputMode.Voice,
            transcriptForApply: "max mustermann",
            ambiguityTelemetry: null,
            sttConfidencePercent: 70,
            CancellationToken.None);

        Assert.True(result.ShouldContinue);
        Assert.False(result.WasSubmitted);
        Assert.Null(result.DialogResult);
        interpreter.Verify(i => i.TryMoveFocusNextAsync(), Times.Never);
        interpreter.VerifyAll();
        telemetry.VerifyAll();
        speechService.VerifyAll();
        router.VerifyNoOtherCalls();
        submissionService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ApplyAsync_WhenSttConfidenceIsSufficient_AndConfirmationIsUndecided_DoesNotAdvanceFocus()
    {
        // Auch bei hoher Konfidenz darf die ausbleibende Bestätigung den Fokus nicht weiterschalten.
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
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
        speechService
            .Setup(s => s.SpeakSafeAsync(It.IsAny<string>(), "de", It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        var submissionService = new Mock<IFormVoiceCommandSubmissionService>(MockBehavior.Strict);
        var service = CreateService(telemetry.Object, speechService.Object, router.Object, submissionService.Object);

        var contract = CreateMinimalContract();
        var context = CreateContext(contract);
        var field = CreateFieldSnapshot();
        var session = new EvaluationSession("session-7", EvaluationChannel.Voice, "app", "app/current");

        telemetry
            .Setup(t => t.TrackFieldAttempt(
                session,
                field.FieldId,
                InputModes.Voice,
                true,
                It.Is<int?>(value => value == 90)));
        telemetry
            .Setup(t => t.TrackRepairAttempt(
                session,
                field.FieldId,
                FormVoiceCommandFieldErrorCodes.UnrecognizedInput));
        // Der Mock erwartet auch einen Reparatureintrag mit NoInput für die ausbleibende Bestätigungsantwort.
        telemetry
            .Setup(t => t.TrackRepairAttempt(
                session,
                field.FieldId,
                FormVoiceCommandFieldErrorCodes.NoInput));
        telemetry
            .Setup(t => t.TrackStateEntry(session, It.IsAny<string>()));

        var interpreter = new Mock<IFormDialogRuntime>(MockBehavior.Strict);
        interpreter
            .Setup(i => i.TryApplyTranscriptAsync("max mustermann"))
            .ReturnsAsync(new FormInputResult(ValueCaptured: true, Violations: []));
        interpreter
            .Setup(i => i.GetValuesSnapshotAsync())
            .ReturnsAsync(ImmutableDictionary.CreateRange(
                StringComparer.OrdinalIgnoreCase,
                [
                    new KeyValuePair<string, string>("name", "max mustermann")
                ]));
        interpreter.As<IFormDialogPrivacyReadbackPolicy>()
            .Setup(i => i.IsSpellOutAllowedInConfirmationAsync("name"))
            .ReturnsAsync(true);

        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        PrepareTrackerForApply(stateTracker, field.FieldGroupId);
        var autoHelp = CreateAutoHelpTracker(interpreter.Object);

        var result = await service.ApplyAsync(
            context,
            interpreter.Object,
            field,
            session,
            stateTracker,
            autoHelp,
            expectedInputMode: InputMode.Voice,
            transcriptForApply: "max mustermann",
            ambiguityTelemetry: null,
            sttConfidencePercent: 90,
            CancellationToken.None);

        Assert.True(result.ShouldContinue);
        Assert.False(result.WasSubmitted);
        Assert.Null(result.DialogResult);
        interpreter.Verify(i => i.TryMoveFocusNextAsync(), Times.Never);
        interpreter.VerifyAll();
        telemetry.VerifyAll();
        speechService.VerifyAll();
        router.VerifyNoOtherCalls();
        submissionService.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("de")]
    [InlineData("en")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("it")]
    public async Task ApplyAsync_WhenFreeTextDictationIsBelowConfirmThreshold_AcceptsAndReadsTheValueBack(string iso)
    {
        // Freitext wird bei 50 Prozent Konfidenz zur Bestätigung weitergegeben.
        // Der angeforderte Bestätigungstext muss den Wert in allen fünf geprüften Sprachen enthalten.
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var capturedConfirmationPrompts = new List<string>();
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Loose);
        speechService.ForwardConfirmationCaptureToValueCapture();
        speechService
            .Setup(s => s.SpeakSafeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
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
                (prompt, _, _, onStartListening, _, _, _, _) =>
                {
                    capturedConfirmationPrompts.Add(prompt);
                    onStartListening();
                    return Task.FromResult<SpeechCaptureResult>(new SpeechCaptureResult("ja", null));
                });

        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        var submissionService = new Mock<IFormVoiceCommandSubmissionService>(MockBehavior.Strict);
        var service = CreateService(telemetry.Object, speechService.Object, router.Object, submissionService.Object);

        var context = SpellingWorkflowTestFactory.CreateContext(iso);
        var field = CreateFieldSnapshot();
        var session = new EvaluationSession($"session-freetext-{iso}", EvaluationChannel.Voice, "app", "app/current");

        var interpreter = new Mock<IFormDialogRuntime>(MockBehavior.Loose);
        interpreter
            .Setup(i => i.TryApplyTranscriptAsync("hallo"))
            .ReturnsAsync(new FormInputResult(ValueCaptured: true, Violations: []));
        interpreter
            .Setup(i => i.GetValuesSnapshotAsync())
            .ReturnsAsync(ImmutableDictionary.CreateRange(
                StringComparer.OrdinalIgnoreCase,
                [
                    new KeyValuePair<string, string>("name", "hallo")
                ]));
        interpreter.As<IFormDialogPrivacyReadbackPolicy>()
            .Setup(i => i.IsSpellOutAllowedInConfirmationAsync("name"))
            .ReturnsAsync(true);
        interpreter
            .Setup(i => i.TryMoveFocusNextAsync())
            .ReturnsAsync(true);
        interpreter
            .Setup(i => i.GetFocusedFieldAsync())
            .ReturnsAsync(field);

        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        PrepareTrackerForApply(stateTracker, field.FieldGroupId);
        var autoHelp = CreateAutoHelpTracker(interpreter.Object);

        var result = await service.ApplyAsync(
            context,
            interpreter.Object,
            field,
            session,
            stateTracker,
            autoHelp,
            expectedInputMode: InputMode.Voice,
            transcriptForApply: "hallo",
            ambiguityTelemetry: null,
            sttConfidencePercent: 50,
            CancellationToken.None);

        Assert.True(result.ShouldContinue);
        interpreter.Verify(i => i.TryApplyTranscriptAsync("hallo"), Times.Once);
        Assert.NotEmpty(capturedConfirmationPrompts);
        Assert.Contains(capturedConfirmationPrompts, prompt => prompt.Contains("hallo", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ApplyAsync_WhenSpellingCaptureOnTextFieldIsAtAboutFiftyPercent_StaysStrictAndEscalates()
    {
        // Die Freitextausnahme gilt nicht im Buchstabiermodus: Bei 50 Prozent Konfidenz bleibt die Wertübernahme aus.
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Loose);
        speechService.ForwardConfirmationCaptureToValueCapture();
        speechService
            .Setup(s => s.SpeakSafeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
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
            .ReturnsAsync(new SpeechCaptureResult(string.Empty, null));

        var earconPlayer = new Mock<IEarconPlayer>(MockBehavior.Loose);
        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        var submissionService = new Mock<IFormVoiceCommandSubmissionService>(MockBehavior.Strict);
        var service = CreateService(telemetry.Object, speechService.Object, router.Object, submissionService.Object, earconPlayer.Object);

        var contract = CreateMinimalContract();
        var context = CreateContext(contract);
        var field = CreateFieldSnapshot();
        var session = new EvaluationSession("session-spelling-text-50", EvaluationChannel.Voice, "app", "app/current");

        var interpreter = new Mock<IFormDialogRuntime>(MockBehavior.Loose);
        interpreter
            .Setup(i => i.GetValuesSnapshotAsync())
            .ReturnsAsync(ImmutableDictionary.Create<string, string>(StringComparer.OrdinalIgnoreCase));
        interpreter.As<IFormDialogPrivacyReadbackPolicy>()
            .Setup(i => i.IsSpellOutAllowedInConfirmationAsync("name"))
            .ReturnsAsync(false);

        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        PrepareTrackerForApply(stateTracker, field.FieldGroupId);
        var autoHelp = CreateAutoHelpTracker(interpreter.Object);

        var result = await service.ApplyAsync(
            context,
            interpreter.Object,
            field,
            session,
            stateTracker,
            autoHelp,
            expectedInputMode: InputMode.Spelling,
            transcriptForApply: "h a l l o",
            ambiguityTelemetry: null,
            sttConfidencePercent: 50,
            CancellationToken.None);

        Assert.True(result.ShouldContinue);
        earconPlayer.Verify(e => e.PlayAsync(EarconId.ValidationAmbiguous, It.IsAny<CancellationToken>()), Times.Once);
        interpreter.Verify(i => i.TryApplyTranscriptAsync(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData(false, ValueTypeKinds.String, false, "privacy-masked free text (P2a)")]
    [InlineData(true, ValueTypeKinds.String, true, "code field (P2b)")]
    [InlineData(false, ValueTypeKinds.Integer, true, "constrained number")]
    [InlineData(false, ValueTypeKinds.Boolean, true, "constrained boolean")]
    public async Task ApplyAsync_WhenNonRelaxableFieldIsBelowConfirmThreshold_StaysStrict(
        bool codeStyle,
        string valueTypeKind,
        bool speakValueInConfirmation,
        string scenario)
    {
        // Die vier Fälle prüfen die Ablehnung bei maskiertem Freitext sowie Code-, Ganzzahl- und Wahrheitswertfeldern.
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Loose);
        speechService.ForwardConfirmationCaptureToValueCapture();
        speechService
            .Setup(s => s.SpeakSafeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        var submissionService = new Mock<IFormVoiceCommandSubmissionService>(MockBehavior.Strict);
        var service = CreateService(telemetry.Object, speechService.Object, router.Object, submissionService.Object);

        var contract = CreateMinimalContract();
        var context = CreateContext(contract);
        var field = CreateFieldSnapshot(
            codeStyle: codeStyle,
            valueTypeKind: valueTypeKind,
            speakValueInConfirmation: speakValueInConfirmation);
        var session = new EvaluationSession("session-strict", EvaluationChannel.Voice, "app", "app/current");

        var interpreter = new Mock<IFormDialogRuntime>(MockBehavior.Strict);
        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        PrepareTrackerForApply(stateTracker, field.FieldGroupId);
        var autoHelp = CreateAutoHelpTracker(interpreter.Object);

        var result = await service.ApplyAsync(
            context,
            interpreter.Object,
            field,
            session,
            stateTracker,
            autoHelp,
            expectedInputMode: InputMode.Voice,
            transcriptForApply: "hallo",
            ambiguityTelemetry: null,
            sttConfidencePercent: 50,
            CancellationToken.None);

        Assert.True(result.ShouldContinue, scenario);
        interpreter.VerifyNoOtherCalls();
        router.VerifyNoOtherCalls();
        submissionService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ApplyAsync_DateParseFailure_DoesNotAppendConstructiveFallbackOffer()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var spokenPrompts = new List<string>();
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(s => s.SpeakSafeAsync(It.IsAny<string>(), "de", It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, VoiceDialogSettings, CancellationToken>((prompt, _, _, _) => spokenPrompts.Add(prompt))
            .Returns(Task.CompletedTask);

        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        var submissionService = new Mock<IFormVoiceCommandSubmissionService>(MockBehavior.Strict);
        var service = CreateService(telemetry.Object, speechService.Object, router.Object, submissionService.Object);

        var contract = CreateMinimalContract();
        var context = CreateContext(contract);
        var field = CreateFieldSnapshot(valueTypeKind: ValueTypeKinds.Date);
        var session = new EvaluationSession("session-date-parse-failure", EvaluationChannel.Voice, "app", "app/current");
        var interpreter = new Mock<IFormDialogRuntime>(MockBehavior.Strict);
        interpreter
            .Setup(i => i.TryApplyTranscriptAsync("achter Januar"))
            .ReturnsAsync(new FormInputResult(ValueCaptured: false, Violations: []));

        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        var autoHelp = CreateAutoHelpTracker(interpreter.Object);
        var escalation = new FormVoiceCommandUnrecognizedInputEscalationPolicy();

        await ApplyRejectedTranscriptAsync(
            service,
            context,
            interpreter.Object,
            field,
            session,
            stateTracker,
            autoHelp,
            escalation,
            "achter Januar");
        await ApplyRejectedTranscriptAsync(
            service,
            context,
            interpreter.Object,
            field,
            session,
            stateTracker,
            autoHelp,
            escalation,
            "achter Januar");

        var expectedOffer = FormDateComponentPromptBuilder.GetDateComponentEntryAvailablePrompt(
            contract,
            context.IsoCode,
            context.VoiceSettings.GetPolitenessLevel());
        Assert.Equal(2, spokenPrompts.Count);
        Assert.DoesNotContain(expectedOffer, spokenPrompts[^1], StringComparison.Ordinal);
        interpreter.Verify(i => i.TryApplyTranscriptAsync("achter Januar"), Times.Exactly(2));
        router.VerifyNoOtherCalls();
        submissionService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ApplyAsync_FreeTextParseFailure_KeepsConstructiveFallbackOffer()
    {
        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Loose);
        var spokenPrompts = new List<string>();
        var speechService = new Mock<IFormVoiceCommandSpeechService>(MockBehavior.Strict);
        speechService
            .Setup(s => s.SpeakSafeAsync(It.IsAny<string>(), "de", It.IsAny<VoiceDialogSettings>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, VoiceDialogSettings, CancellationToken>((prompt, _, _, _) => spokenPrompts.Add(prompt))
            .Returns(Task.CompletedTask);

        var router = new Mock<IFormVoiceCommandGlobalCommandRouter>(MockBehavior.Strict);
        var submissionService = new Mock<IFormVoiceCommandSubmissionService>(MockBehavior.Strict);
        var service = CreateService(telemetry.Object, speechService.Object, router.Object, submissionService.Object);

        var contract = CreateMinimalContract();
        var context = CreateContext(contract);
        var field = CreateFieldSnapshot(valueTypeKind: ValueTypeKinds.String);
        var session = new EvaluationSession("session-free-text-parse-failure", EvaluationChannel.Voice, "app", "app/current");
        var interpreter = new Mock<IFormDialogRuntime>(MockBehavior.Strict);
        interpreter
            .Setup(i => i.TryApplyTranscriptAsync("unverstaendlich"))
            .ReturnsAsync(new FormInputResult(ValueCaptured: false, Violations: []));

        var stateTracker = new FormVoiceCommandStateTracker(telemetry.Object, session);
        var autoHelp = CreateAutoHelpTracker(interpreter.Object);
        var escalation = new FormVoiceCommandUnrecognizedInputEscalationPolicy();

        await ApplyRejectedTranscriptAsync(
            service,
            context,
            interpreter.Object,
            field,
            session,
            stateTracker,
            autoHelp,
            escalation,
            "unverstaendlich");
        await ApplyRejectedTranscriptAsync(
            service,
            context,
            interpreter.Object,
            field,
            session,
            stateTracker,
            autoHelp,
            escalation,
            "unverstaendlich");

        var expectedOffer = FormSpellingOverrideDiscoveryPromptBuilder.GetSpellingOverrideAvailablePrompt(
            contract,
            context.IsoCode,
            context.VoiceSettings.GetPolitenessLevel());
        Assert.Equal(2, spokenPrompts.Count);
        Assert.Contains(expectedOffer, spokenPrompts[^1], StringComparison.Ordinal);
        interpreter.Verify(i => i.TryApplyTranscriptAsync("unverstaendlich"), Times.Exactly(2));
        router.VerifyNoOtherCalls();
        submissionService.VerifyNoOtherCalls();
    }

    private static FormVoiceCommandFieldApplyService CreateService(
        IEvaluationTelemetry telemetry,
        IFormVoiceCommandSpeechService speechService,
        IFormVoiceCommandGlobalCommandRouter commandRouter,
        IFormVoiceCommandSubmissionService submissionService,
        IEarconPlayer? earconPlayer = null)
    {
        var commandExecutionService = new FormVoiceCommandGlobalCommandExecutionService(telemetry, commandRouter);
        var confirmationDecisionService = CreateConfirmationDecisionService(speechService, new FormTranscriptParser(new YesNoTranscriptClassifier(), new ModeledNegationParticleResolver()));
        var earcon = earconPlayer ?? Mock.Of<IEarconPlayer>();
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
            earcon);

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

    private static async Task ApplyRejectedTranscriptAsync(
        FormVoiceCommandFieldApplyService service,
        FormVoiceCommandContext context,
        IFormDialogRuntime interpreter,
        FormFieldSnapshot field,
        EvaluationSession session,
        FormVoiceCommandStateTracker stateTracker,
        FormVoiceCommandAutoHelpTracker autoHelp,
        FormVoiceCommandUnrecognizedInputEscalationPolicy escalation,
        string transcript)
    {
        PrepareTrackerForApply(stateTracker, field.FieldGroupId);
        var turnContext = new FormVoiceCommandFieldTurnContext(
            context,
            interpreter,
            field,
            session,
            stateTracker,
            autoHelp,
            escalation,
            interpreter as IFormDialogPrivacyReadbackPolicy ?? Mock.Of<IFormDialogPrivacyReadbackPolicy>());

        await service.ApplyAsync(
                turnContext,
                new FormVoiceCommandFieldApplyRequest(
                    InputMode.Voice,
                    transcript,
                    ambiguityTelemetry: null,
                    sttConfidencePercent: null),
                CancellationToken.None)
            .ConfigureAwait(false);
    }

    private static FormFieldSnapshot CreateFieldSnapshot(
        bool codeStyle = false,
        string valueTypeKind = ValueTypeKinds.String,
        bool speakValueInConfirmation = true)
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
                CodeStyle: codeStyle,
                InputMode: InputMode.Voice,
                Editability: EditabilityModes.UserEditable,
                ValueTypeKind: valueTypeKind,
                SpeakValueInConfirmation: speakValueInConfirmation));
    }

    private static FormContract CreateMinimalContract(
        VoiceHintsDefinition? voiceHints = null,
        FormVoiceProfile? formVoiceProfile = null)
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
            Projections = voiceHints is null && formVoiceProfile is null
                ? null
                : new ContractProjections
                {
                    Voice = new VoiceProjection
                    {
                        Hints = voiceHints,
                        FormVoiceProfile = formVoiceProfile
                    }
                },
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
