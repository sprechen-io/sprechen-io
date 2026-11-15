using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Moq;
using Sprechen.Speech.App.Contracts.Evaluation;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.ModelRuntime.PromptTemplates;
using Sprechen.Speech.ModelRuntime.Services;
using Sprechen.VoiceAssistant.Orchestration.Dialogs.SpeechAssistant;

namespace Sprechen.VoiceAssistant.Dialogs.Tests.Dialogs.SpeechAssistant;

public sealed class SessionEntryQuestionTests
{
    [Fact]
    public async Task AskAsync_CompletesCaptureWarmUpBeforeSpeakingAndRecording()
    {
        var events = new ConcurrentQueue<string>();
        var warmUpCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var questionSpoken = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var capture = new Mock<ISpeechCaptureClient>(MockBehavior.Strict);
        capture
            .Setup(client => client.EnsureCaptureSessionReadyAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                events.Enqueue("warm-up");
                return warmUpCompletion.Task;
            });
        var modeAwareCapture = SetUpSilentAnswer(events);
        var textToSpeech = new Mock<ITextToSpeechPlayer>(MockBehavior.Strict);
        textToSpeech
            .Setup(player => player.SpeakAsync("Welche Sprache?", "de", It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                events.Enqueue("speak");
                questionSpoken.SetResult();
                return Task.CompletedTask;
            });
        var telemetry = SetUpNoSpeechTelemetry();
        await using var recognition = new SpeechRecognitionWorker(Mock.Of<ISpeechRecognitionClient>());
        var question = CreateQuestion(
            capture.Object,
            modeAwareCapture.Object,
            textToSpeech.Object,
            recognition,
            Mock.Of<ILogger>(),
            telemetry.Object);

        var answerTask = question.AskAsync(
            [new SpokenSegment("Welche Sprache?", "de")],
            "de",
            SessionEntryQuestion.BuildAcceptanceKeyterms("de"),
            TimeSpan.FromSeconds(3),
            CancellationToken.None).AsTask();

        Assert.Equal(["warm-up"], events.ToArray());
        Assert.False(questionSpoken.Task.IsCompleted);
        Assert.False(answerTask.IsCompleted);

        warmUpCompletion.SetResult();
        await questionSpoken.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var answer = await answerTask.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal(SessionEntryAnswer.Silence, answer);
        Assert.Equal(["warm-up", "speak", "record"], events.ToArray());
        capture.Verify(
            client => client.EnsureCaptureSessionReadyAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AskAsync_WhenCaptureWarmUpHasTransientFailure_StillSpeaksAndRecords(
        bool typedUnavailable)
    {
        var events = new ConcurrentQueue<string>();
        Exception warmUpFailure = typedUnavailable
            ? new MicrophoneAcquisitionException(MicrophoneAcquisitionFailureCode.Unavailable)
            : new InvalidOperationException("microphone start failed");
        var capture = new Mock<ISpeechCaptureClient>(MockBehavior.Strict);
        capture
            .Setup(client => client.EnsureCaptureSessionReadyAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(warmUpFailure);
        var modeAwareCapture = SetUpSilentAnswer(events);
        var textToSpeech = new Mock<ITextToSpeechPlayer>(MockBehavior.Strict);
        textToSpeech
            .Setup(player => player.SpeakAsync("Welche Sprache?", "de", It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                events.Enqueue("speak");
                return Task.CompletedTask;
            });
        var logger = new Mock<ILogger>(MockBehavior.Strict);
        logger.Setup(candidate => candidate.Log(
            LogLevel.Warning,
            new EventId(3004, "SessionEntryCaptureWarmUpFailed"),
            It.IsAny<It.IsAnyType>(),
            warmUpFailure,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()));
        var telemetry = SetUpNoSpeechTelemetry();
        await using var recognition = new SpeechRecognitionWorker(Mock.Of<ISpeechRecognitionClient>());
        var question = CreateQuestion(
            capture.Object,
            modeAwareCapture.Object,
            textToSpeech.Object,
            recognition,
            logger.Object,
            telemetry.Object);

        var answer = await question.AskAsync(
            [new SpokenSegment("Welche Sprache?", "de")],
            "de",
            SessionEntryQuestion.BuildAcceptanceKeyterms("de"),
            TimeSpan.FromSeconds(3),
            CancellationToken.None);

        Assert.Equal(SessionEntryAnswer.Silence, answer);
        Assert.Equal(["speak", "record"], events.ToArray());
        logger.VerifyAll();
        capture.VerifyAll();
    }

    [Theory]
    [InlineData(MicrophoneAcquisitionFailureCode.DeviceNotFound)]
    [InlineData(MicrophoneAcquisitionFailureCode.PermissionDenied)]
    public async Task AskAsync_WhenCaptureIsPermanentlyUnavailable_DoesNotAskOrListen(
        MicrophoneAcquisitionFailureCode failureCode)
    {
        var captureFailure = new MicrophoneAcquisitionException(failureCode);
        var capture = new Mock<ISpeechCaptureClient>(MockBehavior.Strict);
        capture
            .Setup(client => client.EnsureCaptureSessionReadyAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(captureFailure);
        var modeAwareCapture = new Mock<IModeAwareSpeechCaptureClient>(MockBehavior.Strict);
        var textToSpeech = new Mock<ITextToSpeechPlayer>(MockBehavior.Strict);
        await using var recognition = new SpeechRecognitionWorker(Mock.Of<ISpeechRecognitionClient>());
        var question = CreateQuestion(
            capture.Object,
            modeAwareCapture.Object,
            textToSpeech.Object,
            recognition,
            Mock.Of<ILogger>(),
            Mock.Of<IEvaluationSessionTelemetry>());

        var thrown = await Assert.ThrowsAsync<MicrophoneAcquisitionException>(() => question.AskAsync(
            [new SpokenSegment("Welche Sprache?", "de")],
            "de",
            SessionEntryQuestion.BuildAcceptanceKeyterms("de"),
            TimeSpan.FromSeconds(3),
            CancellationToken.None).AsTask());

        Assert.Same(captureFailure, thrown);
        capture.VerifyAll();
        textToSpeech.VerifyNoOtherCalls();
        modeAwareCapture.VerifyNoOtherCalls();
    }

    private static Mock<IModeAwareSpeechCaptureClient> SetUpSilentAnswer(
        ConcurrentQueue<string> events)
    {
        var capture = new Mock<IModeAwareSpeechCaptureClient>(MockBehavior.Strict);
        capture
            .Setup(client => client.RecordAsync(
                SpeechCaptureMode.ShortConfirmation,
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                events.Enqueue("record");
                return Task.FromException<CapturedSpeechAudio>(new NoSpeechDetectedException());
            });
        return capture;
    }

    private static Mock<IEvaluationSessionTelemetry> SetUpNoSpeechTelemetry()
    {
        var telemetry = new Mock<IEvaluationSessionTelemetry>(MockBehavior.Strict);
        telemetry.Setup(candidate => candidate.TrackNoSpeechDetected());
        return telemetry;
    }

    private static SessionEntryQuestion CreateQuestion(
        ISpeechCaptureClient capture,
        IModeAwareSpeechCaptureClient modeAwareCapture,
        ITextToSpeechPlayer textToSpeech,
        SpeechRecognitionWorker recognition,
        ILogger logger,
        IEvaluationSessionTelemetry telemetry)
    {
        var indicator = new Mock<ISpeechCaptureRecordingIndicator>(MockBehavior.Strict);
        indicator
            .Setup(candidate => candidate.BeginRecordingAsync())
            .ReturnsAsync(Mock.Of<IAsyncDisposable>());

        return new SessionEntryQuestion(
            new SessionEntryLanguageOfferDependencies(
                new UserLanguageState(),
                Mock.Of<IUserLanguageSettingsWorkflow>(),
                textToSpeech,
                indicator.Object,
                capture,
                modeAwareCapture,
                recognition,
                Mock.Of<ISessionEntryCancelVocabulary>(),
                logger,
                telemetry));
    }
}
