using Sprechen.Proto;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Diagnostics;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.ModelRuntime.PromptTemplates;
using Sprechen.Speech.Runtime.Tests.TestDoubles;
using Sprechen.VoiceAssistant.VoiceRuntime.Dialogs.SpeechAssistant;
using AppSpeechTranscript = Sprechen.Speech.App.Contracts.Services.SpeechTranscript;
using AppRecognitionProfile = Sprechen.Speech.App.Contracts.Services.RecognitionProfile;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

public sealed class FormVoiceCommandSpeechServiceTests
{
    [Fact]
    public async Task CaptureTranscriptAsync_WhenSilent_SpeaksANoInputCuePerElapsedWindow()
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new ScriptedSpeechInputClient();
        for (var i = 0; i < 3; i++)
        {
            await speechClient.EnqueueRecordStepAsync(_ => throw new NoSpeechDetectedException());
        }

        var tts = new RecordingTextToSpeechPlayer();
        var noInputFeedback = new FormVoiceCommandNoInputFeedbackObserver(tts);
        var recordingService = new FormVoiceCommandRecordingService(speechClient, speechClient, noInputFeedback);
        var service = CreateService(messageBus, speechClient, tts, recordingService, noInputFeedback: noInputFeedback);

        await Assert.ThrowsAsync<NoSpeechDetectedException>(() => service.CaptureTranscriptAsync(
            "Name?",
            "de",
            CreateCaptureSettings(new VoiceDialogSettings { AllowBargeIn = false, MaxRetries = 2, NoInputTimeoutMs = 500 }),
            onStartListening: static () => { },
            telemetryContext: null,
            AppRecognitionProfile.Conversational,
            keyterms: null,
            CancellationToken.None));

        // Eingespeiste Stillefehler erzeugen Hinweise vor Versuch 2 und 3; der dritte Fehler beendet das ausgeschöpfte Budget.
        var noInputCue = NoInputCapturePromptTextResolver.GetNoInputPrompt("de");
        Assert.Equal(2, tts.Calls.Count(call => call.Text == noInputCue));
    }

    [Fact]
    public async Task CaptureTranscriptSingleAttemptAsync_WhenSilent_DoesNotSpeakANoInputCue()
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new ScriptedSpeechInputClient();
        await speechClient.EnqueueRecordStepAsync(_ => throw new NoSpeechDetectedException());

        var tts = new RecordingTextToSpeechPlayer();
        var noInputFeedback = new FormVoiceCommandNoInputFeedbackObserver(tts);
        var recordingService = new FormVoiceCommandRecordingService(speechClient, speechClient, noInputFeedback);
        var service = CreateService(messageBus, speechClient, tts, recordingService, noInputFeedback: noInputFeedback);

        await Assert.ThrowsAsync<NoSpeechDetectedException>(() => service.CaptureTranscriptSingleAttemptAsync(
            "Name?",
            "de",
            CreateCaptureSettings(new VoiceDialogSettings { AllowBargeIn = false, MaxRetries = 2, NoInputTimeoutMs = 500 }),
            onStartListening: static () => { },
            telemetryContext: null,
            AppRecognitionProfile.Conversational,
            keyterms: null,
            CancellationToken.None));

        var noInputCue = NoInputCapturePromptTextResolver.GetNoInputPrompt("de");
        Assert.DoesNotContain(tts.Calls, call => call.Text == noInputCue);
    }

    [Fact]
    public async Task CaptureConfirmationTranscriptAsync_WhenSilent_DoesNotSpeakANoInputCue()
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new ScriptedSpeechInputClient();
        await speechClient.EnqueueRecordStepAsync(_ => throw new NoSpeechDetectedException());

        var tts = new RecordingTextToSpeechPlayer();
        var noInputFeedback = new FormVoiceCommandNoInputFeedbackObserver(tts);
        var recordingService = new FormVoiceCommandRecordingService(speechClient, speechClient, noInputFeedback);
        var service = CreateService(messageBus, speechClient, tts, recordingService, noInputFeedback: noInputFeedback);

        await Assert.ThrowsAsync<NoSpeechDetectedException>(() => service.CaptureConfirmationTranscriptAsync(
            "Ist das richtig?",
            "de",
            CreateCaptureSettings(new VoiceDialogSettings { AllowBargeIn = false, MaxRetries = 2, NoInputTimeoutMs = 500 }),
            onStartListening: static () => { },
            telemetryContext: null,
            keyterms: null,
            CancellationToken.None));

        var noInputCue = NoInputCapturePromptTextResolver.GetNoInputPrompt("de");
        Assert.DoesNotContain(tts.Calls, call => call.Text == noInputCue);
    }

    [Fact]
    public async Task ConfirmYesNoAsync_WhenSilent_DoesNotAddANoInputCue_RepromptsEachWindowInstead()
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new ScriptedSpeechInputClient();
        for (var i = 0; i < 3; i++)
        {
            await speechClient.EnqueueRecordStepAsync(_ => throw new NoSpeechDetectedException());
        }

        var tts = new RecordingTextToSpeechPlayer();
        var noInputFeedback = new FormVoiceCommandNoInputFeedbackObserver(tts);
        var recordingService = new FormVoiceCommandRecordingService(speechClient, speechClient, noInputFeedback);
        var service = CreateService(messageBus, speechClient, tts, recordingService, noInputFeedback: noInputFeedback);

        var answer = await service.ConfirmYesNoAsync(
            CreateCaptureSettings(new VoiceDialogSettings { AllowBargeIn = false, MaxRetries = 2, NoInputTimeoutMs = 500 }),
            "Confirm",
            "de",
            telemetryContext: null,
            CancellationToken.None);

        // Die Bestätigungsschleife gibt ihre eigene Rückfrage aus; ein zusätzlicher Beobachterhinweis würde sie verdoppeln.
        Assert.Null(answer);
        var noInputCue = NoInputCapturePromptTextResolver.GetNoInputPrompt("de");
        Assert.DoesNotContain(tts.Calls, call => call.Text == noInputCue);
        Assert.Equal(3, tts.Calls.Count(call => call.Text == "Confirm"));
    }
    [Fact]
    public async Task CaptureTranscriptAsync_WhenConfidenceProvided_UsesConfidencePercentFromProto()
    {
        var messageBus = new SynchronousClientMessageBus();
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 0x1 }]);

        var speechClient = new Mock<ISpeechInputClient>(MockBehavior.Strict);
        speechClient
            .Setup(s => s.SpeechToTextAsync(utterance, "de", It.IsAny<Sprechen.Speech.App.Contracts.Services.RecognitionProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppSpeechTranscript("hallo", 64));

        var textToSpeechPlayer = new Mock<ITextToSpeechPlayer>(MockBehavior.Strict);
        textToSpeechPlayer
            .Setup(t => t.SpeakAsync("Bitte sprechen", "de", 1.25d, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var recordingService = new Mock<IFormVoiceCommandRecordingService>(MockBehavior.Strict);
        recordingService
            .Setup(r => r.RecordUntilSpeechAsync(
                SpeechCaptureMode.Listening,
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<SampleCursor?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(utterance);

        var service = CreateService(
            messageBus,
            speechClient.Object,
            textToSpeechPlayer.Object,
            recordingService.Object);

        var listeningStarted = false;

        var result = await service.CaptureTranscriptAsync(
            "Bitte sprechen",
            "de",
            CreateCaptureSettings(new VoiceDialogSettings { AllowBargeIn = false, SpeechRate = 1.25d }),
            () => listeningStarted = true,
            telemetryContext: null,
            AppRecognitionProfile.Conversational,
            keyterms: null,
            CancellationToken.None);

        Assert.True(listeningStarted);
        Assert.Equal("hallo", result.Transcript);
        Assert.Equal(64, result.SttConfidencePercent);
        speechClient.VerifyAll();
        textToSpeechPlayer.VerifyAll();
        recordingService.VerifyAll();
    }

    [Fact]
    public async Task CaptureTranscriptAsync_WhenConfidenceIsMissing_ReturnsNullPercent()
    {
        var messageBus = new SynchronousClientMessageBus();
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 0x1 }]);

        var speechClient = new Mock<ISpeechInputClient>(MockBehavior.Strict);
        speechClient
            .Setup(s => s.SpeechToTextAsync(utterance, "de", It.IsAny<Sprechen.Speech.App.Contracts.Services.RecognitionProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppSpeechTranscript("hallo", null));

        var textToSpeechPlayer = new Mock<ITextToSpeechPlayer>(MockBehavior.Strict);
        textToSpeechPlayer
            .Setup(t => t.SpeakAsync("Bitte sprechen", "de", It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var recordingService = new Mock<IFormVoiceCommandRecordingService>(MockBehavior.Strict);
        recordingService
            .Setup(r => r.RecordUntilSpeechAsync(
                SpeechCaptureMode.Listening,
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<SampleCursor?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(utterance);

        var service = CreateService(
            messageBus,
            speechClient.Object,
            textToSpeechPlayer.Object,
            recordingService.Object);

        var result = await service.CaptureTranscriptAsync(
            "Bitte sprechen",
            "de",
            CreateCaptureSettings(new VoiceDialogSettings { AllowBargeIn = false }),
            onStartListening: static () => { },
            telemetryContext: null,
            AppRecognitionProfile.Conversational,
            keyterms: null,
            CancellationToken.None);

        Assert.Equal("hallo", result.Transcript);
        Assert.Null(result.SttConfidencePercent);
        speechClient.VerifyAll();
        textToSpeechPlayer.VerifyAll();
        recordingService.VerifyAll();
    }

    [Fact]
    public async Task CaptureTranscriptAsync_WhenRecognitionReturnsEmptyTranscript_ReturnsNoTranscriptResult()
    {
        var messageBus = new SynchronousClientMessageBus();
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 0x1 }]);

        var speechClient = new Mock<ISpeechInputClient>(MockBehavior.Strict);
        speechClient
            .Setup(s => s.SpeechToTextAsync(utterance, "de", It.IsAny<Sprechen.Speech.App.Contracts.Services.RecognitionProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppSpeechTranscript(string.Empty, 12));

        var textToSpeechPlayer = new Mock<ITextToSpeechPlayer>(MockBehavior.Strict);
        textToSpeechPlayer
            .Setup(t => t.SpeakAsync("Bitte sprechen", "de", It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var recordingService = new Mock<IFormVoiceCommandRecordingService>(MockBehavior.Strict);
        recordingService
            .Setup(r => r.RecordUntilSpeechAsync(
                SpeechCaptureMode.Listening,
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<SampleCursor?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(utterance);

        var service = CreateService(
            messageBus,
            speechClient.Object,
            textToSpeechPlayer.Object,
            recordingService.Object);

        var result = await service.CaptureTranscriptAsync(
            "Bitte sprechen",
            "de",
            CreateCaptureSettings(new VoiceDialogSettings { AllowBargeIn = false }),
            onStartListening: static () => { },
            telemetryContext: null,
            AppRecognitionProfile.Conversational,
            keyterms: null,
            CancellationToken.None);

        Assert.Null(result.Transcript);
        Assert.Equal(12, result.SttConfidencePercent);
        Assert.Null(result.FailureCode);
        Assert.Null(result.Recoverable);
        speechClient.VerifyAll();
        textToSpeechPlayer.VerifyAll();
        recordingService.VerifyAll();
    }

    [Fact]
    public async Task CaptureTranscriptAsync_WhenSpeechToTextFails_ReturnsRecoverableFailureCode()
    {
        // Nach erschöpften Erkennungswiederholungen muss das Ergebnis STT_BACKOFF_EXHAUSTED als behebbaren Fehler enthalten.
        var messageBus = new SynchronousClientMessageBus();
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 0x1 }]);

        var speechClient = new Mock<ISpeechInputClient>(MockBehavior.Strict);
        speechClient
            .Setup(s => s.SpeechToTextAsync(utterance, "de", It.IsAny<Sprechen.Speech.App.Contracts.Services.RecognitionProfile>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("stt failed"));

        var textToSpeechPlayer = new Mock<ITextToSpeechPlayer>(MockBehavior.Strict);
        textToSpeechPlayer
            .Setup(t => t.SpeakAsync("Bitte sprechen", "de", It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var recordingService = new Mock<IFormVoiceCommandRecordingService>(MockBehavior.Strict);
        recordingService
            .Setup(r => r.RecordUntilSpeechAsync(
                SpeechCaptureMode.Listening,
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<SampleCursor?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(utterance);

        var service = CreateService(
            messageBus,
            speechClient.Object,
            textToSpeechPlayer.Object,
            recordingService.Object);

        var result = await service.CaptureTranscriptAsync(
            "Bitte sprechen",
            "de",
            CreateCaptureSettings(new VoiceDialogSettings { AllowBargeIn = false }),
            onStartListening: static () => { },
            telemetryContext: null,
            AppRecognitionProfile.Conversational,
            keyterms: null,
            CancellationToken.None);

        Assert.Null(result.Transcript);
        Assert.Equal(SpeechRecognitionBackoffExhaustedException.CodeValue, result.FailureCode);
        Assert.True(result.Recoverable);
        textToSpeechPlayer.VerifyAll();
        recordingService.VerifyAll();
    }

    [Fact]
    public async Task CaptureTranscriptAsync_WhenPromptPlaybackFails_Rethrows()
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new Mock<ISpeechInputClient>(MockBehavior.Strict);

        var textToSpeechPlayer = new Mock<ITextToSpeechPlayer>(MockBehavior.Strict);
        textToSpeechPlayer
            .Setup(t => t.SpeakAsync("Bitte sprechen", "de", It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("tts failed"));

        var recordingService = new Mock<IFormVoiceCommandRecordingService>(MockBehavior.Strict);

        var service = CreateService(
            messageBus,
            speechClient.Object,
            textToSpeechPlayer.Object,
            recordingService.Object);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CaptureTranscriptAsync(
            "Bitte sprechen",
            "de",
            CreateCaptureSettings(new VoiceDialogSettings { AllowBargeIn = false }),
            onStartListening: static () => { },
            telemetryContext: null,
            AppRecognitionProfile.Conversational,
            keyterms: null,
            CancellationToken.None));

        Assert.Contains("tts failed", exception.Message, StringComparison.Ordinal);
        speechClient.VerifyNoOtherCalls();
        textToSpeechPlayer.VerifyAll();
        recordingService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CaptureTranscriptAsync_WhenBargeInDisabled_WaitsForPromptCompletionBeforeRecording()
    {
        var messageBus = new SynchronousClientMessageBus();
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 0x1 }]);
        var promptCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var order = new List<string>();

        var speechClient = new Mock<ISpeechInputClient>(MockBehavior.Strict);
        speechClient
            .Setup(s => s.SpeechToTextAsync(utterance, "de", It.IsAny<Sprechen.Speech.App.Contracts.Services.RecognitionProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppSpeechTranscript("hallo", null));

        var textToSpeechPlayer = new Mock<ITextToSpeechPlayer>(MockBehavior.Strict);
        textToSpeechPlayer
            .Setup(t => t.SpeakAsync("Bitte sprechen", "de", It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                order.Add("prompt:start");
                await promptCompleted.Task.ConfigureAwait(false);
                order.Add("prompt:end");
            });

        var recordingService = new Mock<IFormVoiceCommandRecordingService>(MockBehavior.Strict);
        recordingService
            .Setup(r => r.RecordUntilSpeechAsync(
                SpeechCaptureMode.Listening,
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<SampleCursor?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                order.Add("record");
                return utterance;
            });

        var service = CreateService(
            messageBus,
            speechClient.Object,
            textToSpeechPlayer.Object,
            recordingService.Object);

        var captureTask = service.CaptureTranscriptAsync(
            "Bitte sprechen",
            "de",
            CreateCaptureSettings(new VoiceDialogSettings { AllowBargeIn = false }),
            onStartListening: static () => { },
            telemetryContext: null,
            AppRecognitionProfile.Conversational,
            keyterms: null,
            CancellationToken.None);

        await Task.Yield();

        Assert.Equal(["prompt:start"], order);
        recordingService.Verify(
            r => r.RecordUntilSpeechAsync(
                It.IsAny<SpeechCaptureMode>(),
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<SampleCursor?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        promptCompleted.TrySetResult();

        var result = await captureTask;

        Assert.Equal("hallo", result.Transcript);
        Assert.Equal(["prompt:start", "prompt:end", "record"], order);
        speechClient.VerifyAll();
        textToSpeechPlayer.VerifyAll();
        recordingService.VerifyAll();
    }

    [Fact]
    public async Task CaptureTranscriptAsync_WhenBargeInEnabled_StartsRecordingBeforePromptCompletes()
    {
        var messageBus = new SynchronousClientMessageBus();
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 0x1 }]);
        var promptCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var recordingStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var speechClient = new Mock<ISpeechInputClient>(MockBehavior.Strict);
        speechClient
            .Setup(s => s.SpeechToTextAsync(utterance, "de", It.IsAny<Sprechen.Speech.App.Contracts.Services.RecognitionProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppSpeechTranscript("hallo", null));

        var textToSpeechPlayer = new Mock<ITextToSpeechPlayer>(MockBehavior.Strict);
        textToSpeechPlayer
            .Setup(t => t.SpeakAsync("Bitte sprechen", "de", It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .Returns(() => promptCompleted.Task);

        var recordingService = new Mock<IFormVoiceCommandRecordingService>(MockBehavior.Strict);
        recordingService
            .Setup(r => r.RecordUntilSpeechAsync(
                SpeechCaptureMode.PromptInterruption,
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                recordingStarted.TrySetResult();
                return utterance;
            });

        var service = CreateService(
            messageBus,
            speechClient.Object,
            textToSpeechPlayer.Object,
            recordingService.Object);

        var listeningStarted = false;

        var captureTask = service.CaptureTranscriptAsync(
            "Bitte sprechen",
            "de",
            CreateCaptureSettings(new VoiceDialogSettings { AllowBargeIn = true }, pauseAfterPromptMs: 250),
            () => listeningStarted = true,
            telemetryContext: null,
            AppRecognitionProfile.Conversational,
            keyterms: null,
            CancellationToken.None);

        await recordingStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.True(listeningStarted);
        Assert.False(captureTask.IsCompleted);

        promptCompleted.TrySetResult();

        var result = await captureTask;

        Assert.Equal("hallo", result.Transcript);
        speechClient.VerifyAll();
        textToSpeechPlayer.VerifyAll();
        recordingService.VerifyAll();
    }

    [Fact]
    public async Task CaptureTranscriptAsync_WhenPromptInterruptedBySpeech_TracksBargeInOnce()
    {
        var messageBus = new SynchronousClientMessageBus();
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 0x1 }]);
        var promptCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var recordingStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var session = new EvaluationSession("session-1", EvaluationChannel.Voice, "app", "app/form");

        var speechClient = new Mock<ISpeechInputClient>(MockBehavior.Strict);
        speechClient
            .Setup(s => s.SpeechToTextAsync(utterance, "de", It.IsAny<Sprechen.Speech.App.Contracts.Services.RecognitionProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppSpeechTranscript("hallo", null));

        var textToSpeechPlayer = new Mock<ITextToSpeechPlayer>(MockBehavior.Strict);
        textToSpeechPlayer
            .Setup(t => t.SpeakAsync("Bitte sprechen", "de", It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .Returns(() => promptCompletion.Task);

        var recordingService = new Mock<IFormVoiceCommandRecordingService>(MockBehavior.Strict);
        recordingService
            .Setup(r => r.RecordUntilSpeechAsync(
                SpeechCaptureMode.PromptInterruption,
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                recordingStarted.TrySetResult();
                _ = messageBus.PublishBargeInAsync();
                promptCompletion.TrySetCanceled();
                return utterance;
            });

        var telemetry = new Mock<IEvaluationTelemetry>(MockBehavior.Strict);
        telemetry
            .Setup(t => t.TrackBargeIn(session, null));

        var service = CreateService(
            messageBus,
            speechClient.Object,
            textToSpeechPlayer.Object,
            recordingService.Object,
            telemetry.Object);

        var result = await service.CaptureTranscriptAsync(
            "Bitte sprechen",
            "de",
            CreateCaptureSettings(new VoiceDialogSettings { AllowBargeIn = true }),
            onStartListening: static () => { },
            new SpeechCaptureTelemetryContext(session),
            AppRecognitionProfile.Conversational,
            keyterms: null,
            CancellationToken.None);

        await recordingStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.Equal("hallo", result.Transcript);
        telemetry.VerifyAll();
        speechClient.VerifyAll();
        textToSpeechPlayer.VerifyAll();
        recordingService.VerifyAll();
    }

    [Fact]
    public async Task CaptureTranscriptAsync_WhenPauseAfterPromptConfigured_WaitsBeforeRecording()
    {
        var messageBus = new SynchronousClientMessageBus();
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 0x1 }]);
        var recordingStarted = new TaskCompletionSource<TimeSpan>(TaskCreationOptions.RunContinuationsAsynchronously);

        var speechClient = new Mock<ISpeechInputClient>(MockBehavior.Strict);
        speechClient
            .Setup(s => s.SpeechToTextAsync(utterance, "de", It.IsAny<Sprechen.Speech.App.Contracts.Services.RecognitionProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppSpeechTranscript("hallo", null));

        var textToSpeechPlayer = new Mock<ITextToSpeechPlayer>(MockBehavior.Strict);
        textToSpeechPlayer
            .Setup(t => t.SpeakAsync("Bitte sprechen", "de", It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var recordingService = new Mock<IFormVoiceCommandRecordingService>(MockBehavior.Strict);
        var stopwatch = Stopwatch.StartNew();
        recordingService
            .Setup(r => r.RecordUntilSpeechAsync(
                SpeechCaptureMode.Listening,
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<SampleCursor?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                recordingStarted.TrySetResult(stopwatch.Elapsed);
                return utterance;
            });

        var service = CreateService(
            messageBus,
            speechClient.Object,
            textToSpeechPlayer.Object,
            recordingService.Object);

        var captureTask = service.CaptureTranscriptAsync(
            "Bitte sprechen",
            "de",
            CreateCaptureSettings(new VoiceDialogSettings { AllowBargeIn = false }, pauseAfterPromptMs: 150),
            onStartListening: static () => { },
            telemetryContext: null,
            AppRecognitionProfile.Conversational,
            keyterms: null,
            CancellationToken.None);

        var startedAfter = await recordingStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var result = await captureTask;

        Assert.Equal("hallo", result.Transcript);
        Assert.True(startedAfter >= TimeSpan.FromMilliseconds(100), $"Recording started too early after {startedAfter.TotalMilliseconds} ms.");
        speechClient.VerifyAll();
        textToSpeechPlayer.VerifyAll();
        recordingService.VerifyAll();
    }

    [Fact]
    public async Task CaptureTranscriptAsync_WhenCancelledDuringPauseAfterPrompt_DoesNotStartRecording()
    {
        var messageBus = new SynchronousClientMessageBus();
        using var cancellationTokenSource = new CancellationTokenSource();

        var speechClient = new Mock<ISpeechInputClient>(MockBehavior.Strict);

        var textToSpeechPlayer = new Mock<ITextToSpeechPlayer>(MockBehavior.Strict);
        textToSpeechPlayer
            .Setup(t => t.SpeakAsync("Bitte sprechen", "de", It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var recordingService = new Mock<IFormVoiceCommandRecordingService>(MockBehavior.Strict);

        var service = CreateService(
            messageBus,
            speechClient.Object,
            textToSpeechPlayer.Object,
            recordingService.Object);

        var captureTask = service.CaptureTranscriptAsync(
            "Bitte sprechen",
            "de",
            CreateCaptureSettings(new VoiceDialogSettings { AllowBargeIn = false }, pauseAfterPromptMs: 1000),
            onStartListening: static () => { },
            telemetryContext: null,
            AppRecognitionProfile.Conversational,
            keyterms: null,
            cancellationTokenSource.Token);

        await Task.Yield();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => captureTask);
        speechClient.VerifyNoOtherCalls();
        textToSpeechPlayer.VerifyAll();
        recordingService.Verify(
            r => r.RecordUntilSpeechAsync(
                It.IsAny<SpeechCaptureMode>(),
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<SampleCursor?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ConfirmYesNoAsync_WhenUserIsSilent_ConsumesExactlyBudgetCaptureAttempts_WithoutMultiplying()
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new ScriptedSpeechInputClient();

        // 20 vorbereitete Stillefehler machen verschachtelte Wiederholungsschleifen zählbar.
        // MaxRetries 3 erlaubt vier Versuche, nicht vier mal vier.
        for (var i = 0; i < 20; i++)
        {
            await speechClient.EnqueueRecordStepAsync(_ => throw new NoSpeechDetectedException());
        }

        var textToSpeechPlayer = new Mock<ITextToSpeechPlayer>(MockBehavior.Strict);
        textToSpeechPlayer
            .Setup(t => t.SpeakAsync("Bitte bestaetigen", "de", It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var recordingService = new FormVoiceCommandRecordingService(speechClient, speechClient);
        var service = CreateService(messageBus, speechClient, textToSpeechPlayer.Object, recordingService);

        var result = await service.ConfirmYesNoAsync(
            CreateCaptureSettings(new VoiceDialogSettings { AllowBargeIn = false, MaxRetries = 3, NoInputTimeoutMs = 500 }),
            "Bitte bestaetigen",
            "de",
            telemetryContext: null,
            CancellationToken.None);

        Assert.Null(result);

        Assert.Equal(4, speechClient.RecordCallCount);
    }

    [Theory]
    [MemberData(nameof(SupportedLanguageIsoCodes))]
    public async Task ConfirmYesNoAsync_ParsesAffirmativeAndNegativeWords_ForEverySupportedLanguage(string isoCode)
    {
        Assert.Equal<bool?>(true, await ConfirmSingleSpokenAnswerAsync(isoCode, YesNo.GetYesWord(isoCode)));
        Assert.Equal<bool?>(false, await ConfirmSingleSpokenAnswerAsync(isoCode, YesNo.GetNoWord(isoCode)));
    }

    [Theory]
    [MemberData(nameof(SupportedLanguageIsoCodes))]
    public async Task ConfirmYesNoAsync_WhenAnswerUnrecognizedThenAffirmative_RePromptsWithinBudget_ForEverySupportedLanguage(string isoCode)
    {
        const string unparseableAnswer = "blubberdiblub";

        // Die erste Antwort wird ausdrücklich als nicht interpretierbar geprüft, damit kein zufälliger Ja/Nein-Treffer die erneute Nachfrage verdeckt.
        Assert.False(YesNo.TryParseYesNo(unparseableAnswer, isoCode, out _));

        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new ScriptedSpeechInputClient();
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 0x1 }]);
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(utterance));
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(utterance));
        speechClient.EnqueueTranscript(unparseableAnswer, 70);
        speechClient.EnqueueTranscript(YesNo.GetYesWord(isoCode), 95);

        var textToSpeechPlayer = new Mock<ITextToSpeechPlayer>(MockBehavior.Strict);
        textToSpeechPlayer
            .Setup(t => t.SpeakAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var recordingService = new FormVoiceCommandRecordingService(speechClient, speechClient);
        var service = CreateService(messageBus, speechClient, textToSpeechPlayer.Object, recordingService);

        var result = await service.ConfirmYesNoAsync(
            CreateCaptureSettings(new VoiceDialogSettings { AllowBargeIn = false, MaxRetries = 2, NoInputTimeoutMs = 500 }),
            "Confirm",
            isoCode,
            telemetryContext: null,
            CancellationToken.None);

        Assert.Equal<bool?>(true, result);

        textToSpeechPlayer.Verify(
            t => t.SpeakAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        Assert.Equal(2, speechClient.RecordCallCount);
    }

    [Fact]
    public async Task ConfirmYesNoAsync_WhenAnswerUnrecognized_RaisesAttemptObserverCarryingUnrecognizedAnswerFailure()
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new ScriptedSpeechInputClient();
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 0x1 }]);
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(utterance));
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(utterance));
        speechClient.EnqueueTranscript("blubberdiblub", 70);
        speechClient.EnqueueTranscript(YesNo.GetYesWord("de"), 95);

        var attempts = new List<FormVoiceCommandAttempt>();
        var observer = new Mock<IFormVoiceCommandAttemptObserver>();
        observer
            .Setup(o => o.OnAttemptStartingAsync(It.IsAny<FormVoiceCommandAttempt>(), It.IsAny<CancellationToken>()))
            .Callback<FormVoiceCommandAttempt, CancellationToken>((attempt, _) => attempts.Add(attempt))
            .Returns(Task.CompletedTask);

        var textToSpeechPlayer = new Mock<ITextToSpeechPlayer>(MockBehavior.Strict);
        textToSpeechPlayer
            .Setup(t => t.SpeakAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var recordingService = new FormVoiceCommandRecordingService(speechClient, speechClient, observer.Object);
        var service = CreateService(messageBus, speechClient, textToSpeechPlayer.Object, recordingService);

        var result = await service.ConfirmYesNoAsync(
            CreateCaptureSettings(new VoiceDialogSettings { AllowBargeIn = false, MaxRetries = 2, NoInputTimeoutMs = 500 }),
            "Bitte bestaetigen",
            "de",
            telemetryContext: null,
            CancellationToken.None);

        Assert.Equal<bool?>(true, result);
        Assert.Equal(
            [
                new FormVoiceCommandAttempt(1, 3, FormVoiceCommandAttemptFailure.None),
                new FormVoiceCommandAttempt(2, 3, FormVoiceCommandAttemptFailure.UnrecognizedAnswer),
            ],
            attempts);
    }

    [Theory]
    [MemberData(nameof(SupportedLanguageIsoCodes))]
    public async Task ConfirmYesNoAsync_WhenBargeInEnabled_RecordsOnePromptInterruptionAttemptPerTurn_ForEverySupportedLanguage(string isoCode)
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new ScriptedSpeechInputClient();
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 0x1 }]);
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(utterance));
        speechClient.EnqueueTranscript(YesNo.GetYesWord(isoCode), 95);

        var textToSpeechPlayer = new Mock<ITextToSpeechPlayer>(MockBehavior.Strict);
        textToSpeechPlayer
            .Setup(t => t.SpeakAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var recordingService = new FormVoiceCommandRecordingService(speechClient, speechClient);
        var service = CreateService(messageBus, speechClient, textToSpeechPlayer.Object, recordingService);

        var result = await service.ConfirmYesNoAsync(
            CreateCaptureSettings(new VoiceDialogSettings { AllowBargeIn = true, MaxRetries = 2, NoInputTimeoutMs = 500 }),
            "Confirm",
            isoCode,
            telemetryContext: null,
            CancellationToken.None);

        // Auch bei erlaubtem Dazwischensprechen verwendet die Bestätigung genau einen Versuch im Modus PromptInterruption.
        Assert.Equal<bool?>(true, result);
        Assert.Equal(1, speechClient.RecordCallCount);
        Assert.Equal([SpeechCaptureMode.PromptInterruption], speechClient.RecordModes);
    }

    [Fact]
    public async Task ConfirmYesNoAsync_WhenCapturedTranscriptIsBlank_RaisesAttemptObserverWithUnrecognizedAnswerNotNoInput()
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new ScriptedSpeechInputClient();
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 0x1 }]);
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(utterance));
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(utterance));

        // Aufgenommenes Audio ohne Erkennungstext ist UnrecognizedAnswer, kein Ablauf der Anfangsstillefrist.
        speechClient.EnqueueTranscript(string.Empty, 12);
        speechClient.EnqueueTranscript(YesNo.GetYesWord("de"), 95);

        var attempts = new List<FormVoiceCommandAttempt>();
        var observer = new Mock<IFormVoiceCommandAttemptObserver>();
        observer
            .Setup(o => o.OnAttemptStartingAsync(It.IsAny<FormVoiceCommandAttempt>(), It.IsAny<CancellationToken>()))
            .Callback<FormVoiceCommandAttempt, CancellationToken>((attempt, _) => attempts.Add(attempt))
            .Returns(Task.CompletedTask);

        var textToSpeechPlayer = new Mock<ITextToSpeechPlayer>(MockBehavior.Strict);
        textToSpeechPlayer
            .Setup(t => t.SpeakAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var recordingService = new FormVoiceCommandRecordingService(speechClient, speechClient, observer.Object);
        var service = CreateService(messageBus, speechClient, textToSpeechPlayer.Object, recordingService);

        var result = await service.ConfirmYesNoAsync(
            CreateCaptureSettings(new VoiceDialogSettings { AllowBargeIn = false, MaxRetries = 2, NoInputTimeoutMs = 500 }),
            "Bitte bestaetigen",
            "de",
            telemetryContext: null,
            CancellationToken.None);

        Assert.Equal<bool?>(true, result);
        Assert.Equal(
            [
                new FormVoiceCommandAttempt(1, 3, FormVoiceCommandAttemptFailure.None),
                new FormVoiceCommandAttempt(2, 3, FormVoiceCommandAttemptFailure.UnrecognizedAnswer),
            ],
            attempts);
    }

    [Fact]
    public async Task ConfirmYesNoAsync_WhenBargeInDisabled_RecordsShortConfirmationModeNotListening()
    {
        // Bestätigungen ohne Dazwischensprechen verwenden ShortConfirmation statt des strengeren Listening-Modus.
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new ScriptedSpeechInputClient();
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 0x1 }]);
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(utterance));
        speechClient.EnqueueTranscript(YesNo.GetYesWord("de"), 95);

        var textToSpeechPlayer = new Mock<ITextToSpeechPlayer>(MockBehavior.Strict);
        textToSpeechPlayer
            .Setup(t => t.SpeakAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var recordingService = new FormVoiceCommandRecordingService(speechClient, speechClient);
        var service = CreateService(messageBus, speechClient, textToSpeechPlayer.Object, recordingService);

        var result = await service.ConfirmYesNoAsync(
            CreateCaptureSettings(new VoiceDialogSettings { AllowBargeIn = false, MaxRetries = 2, NoInputTimeoutMs = 500 }),
            "Confirm",
            "de",
            telemetryContext: null,
            CancellationToken.None);

        Assert.Equal<bool?>(true, result);
        Assert.Equal([SpeechCaptureMode.ShortConfirmation], speechClient.RecordModes);
    }

    [Fact]
    public async Task CaptureConfirmationTranscriptAsync_RecordsShortConfirmationMode_NotListening()
    {
        // Die Antwort übernehmen bleibt bei der Wertbestätigung erhalten; geprüft wird der Modus ShortConfirmation, keine akustische Erkennung.
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new ScriptedSpeechInputClient();
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 0x1 }]);
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(utterance));
        speechClient.EnqueueTranscript("übernehmen", 95);

        var textToSpeechPlayer = new Mock<ITextToSpeechPlayer>(MockBehavior.Strict);
        textToSpeechPlayer
            .Setup(t => t.SpeakAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var recordingService = new FormVoiceCommandRecordingService(speechClient, speechClient);
        var service = CreateService(messageBus, speechClient, textToSpeechPlayer.Object, recordingService);

        var result = await service.CaptureConfirmationTranscriptAsync(
            "Name ist Max. Zum Übernehmen sagen Sie übernehmen.",
            "de",
            CreateCaptureSettings(new VoiceDialogSettings { AllowBargeIn = false, MaxRetries = 2, NoInputTimeoutMs = 500 }),
            onStartListening: static () => { },
            telemetryContext: null,
            keyterms: null,
            CancellationToken.None);

        Assert.Equal("übernehmen", result.Transcript);
        Assert.Equal([SpeechCaptureMode.ShortConfirmation], speechClient.RecordModes);
    }

    public static TheoryData<string> SupportedLanguageIsoCodes()
    {
        var data = new TheoryData<string>();
        foreach (var isoCode in VoiceInterfaceModelCatalog.SupportedLanguages)
        {
            data.Add(isoCode);
        }

        return data;
    }

    private static async Task<bool?> ConfirmSingleSpokenAnswerAsync(string isoCode, string spokenAnswer)
    {
        var messageBus = new SynchronousClientMessageBus();
        var speechClient = new ScriptedSpeechInputClient();
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 0x1 }]);
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(utterance));
        speechClient.EnqueueTranscript(spokenAnswer, 95);

        var textToSpeechPlayer = new Mock<ITextToSpeechPlayer>(MockBehavior.Strict);
        textToSpeechPlayer
            .Setup(t => t.SpeakAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var recordingService = new FormVoiceCommandRecordingService(speechClient, speechClient);
        var service = CreateService(messageBus, speechClient, textToSpeechPlayer.Object, recordingService);

        return await service.ConfirmYesNoAsync(
            CreateCaptureSettings(new VoiceDialogSettings { AllowBargeIn = false, MaxRetries = 2, NoInputTimeoutMs = 500 }),
            "Confirm",
            isoCode,
            telemetryContext: null,
            CancellationToken.None);
    }

    [Fact]
    public async Task ReplayBuffer_RecordsSpokenFeedbackButNotTheCaptureReprompt()
    {
        var messageBus = new SynchronousClientMessageBus();
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 0x1 }]);

        var speechClient = new Mock<ISpeechInputClient>(MockBehavior.Strict);
        speechClient
            .Setup(s => s.SpeechToTextAsync(utterance, "de", It.IsAny<Sprechen.Speech.App.Contracts.Services.RecognitionProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppSpeechTranscript("12345", 90));

        var textToSpeechPlayer = new Mock<ITextToSpeechPlayer>(MockBehavior.Loose);
        textToSpeechPlayer
            .Setup(t => t.SpeakAsync(It.IsAny<string>(), "de", It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var recordingService = new Mock<IFormVoiceCommandRecordingService>(MockBehavior.Strict);
        recordingService
            .Setup(r => r.RecordUntilSpeechAsync(
                SpeechCaptureMode.Listening,
                It.IsAny<VoiceDialogSettings>(),
                It.IsAny<SampleCursor?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(utterance);

        var replayBuffer = new FormVoiceCommandSpokenOutputReplayBuffer();
        var service = CreateService(
            messageBus,
            speechClient.Object,
            textToSpeechPlayer.Object,
            recordingService.Object,
            replayBuffer: replayBuffer);

        // Die erneute Feldfrage darf die gespeicherte Validierungsmeldung nicht verdrängen,
        // damit repeat weiterhin diese Fehlermeldung ausgibt.
        await service.SpeakSafeAsync(
            "Die Postleitzahl ist ungueltig.",
            "de",
            new VoiceDialogSettings(),
            CancellationToken.None);

        Assert.Equal("Die Postleitzahl ist ungueltig.", replayBuffer.LastSpokenOutput);

        await service.CaptureTranscriptAsync(
            "Postleitzahl?",
            "de",
            CreateCaptureSettings(new VoiceDialogSettings { AllowBargeIn = false }),
            onStartListening: static () => { },
            telemetryContext: null,
            AppRecognitionProfile.Conversational,
            keyterms: null,
            CancellationToken.None);

        Assert.Equal("Die Postleitzahl ist ungueltig.", replayBuffer.LastSpokenOutput);
    }

    private static FormVoiceCommandSpeechService CreateService(
        IClientMessageBus messageBus,
        ISpeechRecognitionClient speechRecognitionClient,
        ITextToSpeechPlayer textToSpeechPlayer,
        IFormVoiceCommandRecordingService recordingService,
        IEvaluationTelemetry? telemetry = null,
        FormVoiceCommandNoInputFeedbackObserver? noInputFeedback = null,
        IFormVoiceCommandSpokenOutputReplayBuffer? replayBuffer = null)
    {
        ArgumentNullException.ThrowIfNull(messageBus);
        ArgumentNullException.ThrowIfNull(speechRecognitionClient);
        ArgumentNullException.ThrowIfNull(textToSpeechPlayer);
        ArgumentNullException.ThrowIfNull(recordingService);

        var signals = SpeechInteractionSignalsTestFactory.Create(messageBus);
        return new FormVoiceCommandSpeechService(
            telemetry ?? Mock.Of<IEvaluationTelemetry>(),
            signals,
            signals,
            new SpeechRecognitionWorker(speechRecognitionClient),
            textToSpeechPlayer,
            recordingService,
            NullLogger<FormVoiceCommandSpeechService>.Instance,
            noInputFeedback,
            replayBuffer);
    }

    private static FormVoiceCommandCaptureSettings CreateCaptureSettings(
        VoiceDialogSettings voiceSettings,
        int pauseAfterPromptMs = 0)
        => new(
            voiceSettings,
            pauseAfterPromptMs,
            SpeechCapturePromptPresentation.Spoken,
            SpeechInputAcceptanceFeedback.Standard);
}
