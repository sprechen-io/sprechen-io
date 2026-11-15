using Moq;
using Sprechen.Mda.Contracts.V1.Catalog;
using Sprechen.Speech.DialogRuntime.Contracts.Dialogs.SpeechAssistant;
using Sprechen.Speech.Runtime.Tests.TestDoubles;

namespace Sprechen.Speech.Runtime.Tests.Dialogs.SpeechAssistant;

public sealed class FormVoiceCommandRecordingServiceTests
{
    [Fact]
    public async Task RecordUntilSpeechAsync_WhenPromptInterruptionRequested_UsesPromptInterruptionMode()
    {
        var speechClient = new ScriptedSpeechInputClient();
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [Array.Empty<byte>()]);
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(utterance));

        var service = new FormVoiceCommandRecordingService(speechClient, speechClient);

        var result = await service.RecordUntilSpeechAsync(
            SpeechCaptureMode.PromptInterruption,
            CreateVoiceSettings(),
            CancellationToken.None);

        Assert.Same(utterance, result);
        Assert.Equal([SpeechCaptureMode.PromptInterruption], speechClient.RecordModes);
    }

    [Fact]
    public async Task RecordUntilSpeechAsync_WhenListeningRequested_UsesModeAwareListeningCapture()
    {
        var speechClient = new ScriptedSpeechInputClient();
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 0x1 }]);
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(utterance));

        var service = new FormVoiceCommandRecordingService(speechClient, speechClient);

        var result = await service.RecordUntilSpeechAsync(
            SpeechCaptureMode.Listening,
            CreateVoiceSettings(),
            CancellationToken.None);

        Assert.Same(utterance, result);
        Assert.Equal([SpeechCaptureMode.Listening], speechClient.RecordModes);
    }

    [Fact]
    public void Constructor_WhenModeAwareClientIsNull_Throws()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new FormVoiceCommandRecordingService(Mock.Of<ISpeechCaptureClient>(), modeAwareSpeechCaptureClient: null!));

        Assert.Equal("modeAwareSpeechCaptureClient", exception.ParamName);
    }

    [Fact]
    public async Task RecordUntilSpeechAsync_WhenStartCursorProvided_RoutesListeningThroughCursorHandoffOverload()
    {
        var speechClient = new ScriptedSpeechInputClient();
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 0x1 }]);
        await speechClient.EnqueueRecordStepAsync(_ => Task.FromResult(utterance));

        var service = new FormVoiceCommandRecordingService(speechClient, speechClient);

        var result = await service.RecordUntilSpeechAsync(
            SpeechCaptureMode.Listening,
            CreateVoiceSettings(),
            startCursor: new SampleCursor(7),
            CancellationToken.None);

        Assert.Same(utterance, result);
        Assert.Equal([SpeechCaptureMode.Listening], speechClient.RecordModes);

        // Auch die Überladung mit Cursor muss die Anfangsstillefrist je Versuch weitergeben.
        Assert.Equal([(TimeSpan?)TimeSpan.FromMilliseconds(500)], speechClient.InitialSilenceTimeouts);
    }

    [Fact]
    public async Task RecordSingleAttemptAsync_WhenConfirmedUtteranceOutlastsNoInputDeadline_IsNotCancelledAsNoInput()
    {
        // Die Anfangsstillefrist gehört zum Segmentierer und endet bei bestätigtem Sprachbeginn.
        // Eine Zeitgrenze für den gesamten Versuch würde die hier länger laufende Aufnahme abbrechen.
        // Der Test simuliert diese laufende Aufnahme, ohne echte Sprache oder Segmentierung zu messen.
        var speechClient = new ScriptedSpeechInputClient();
        var utterance = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 0x1 }]);
        var releaseCapture = new TaskCompletionSource();
        await speechClient.EnqueueRecordStepAsync(async ct =>
        {
            await releaseCapture.Task.WaitAsync(ct).ConfigureAwait(false);
            return utterance;
        });

        var service = new FormVoiceCommandRecordingService(speechClient, speechClient);

        var recordTask = service.RecordSingleAttemptAsync(
            SpeechCaptureMode.Listening,
            new VoiceDialogSettings { AllowBargeIn = true, MaxRetries = 0, NoInputTimeoutMs = 100 },
            startCursor: null,
            new FormVoiceCommandAttempt(1, 1, FormVoiceCommandAttemptFailure.None),
            CancellationToken.None);

        await Task.Delay(400);
        releaseCapture.SetResult();

        var result = await recordTask;
        Assert.Same(utterance, result);

        Assert.Equal([(TimeSpan?)TimeSpan.FromMilliseconds(100)], speechClient.InitialSilenceTimeouts);
    }

    [Fact]
    public void SnapshotLiveCaptureCursor_DelegatesToModeAwareClient()
    {
        var modeAwareClient = new ScriptedSpeechInputClient();
        var withModeAware = new FormVoiceCommandRecordingService(modeAwareClient, modeAwareClient);

        Assert.NotNull(withModeAware.SnapshotLiveCaptureCursor());
    }

    [Fact]
    public async Task RecordUntilSpeechAsync_FiresAttemptObserverPerAttempt_WithIndexBudgetAndPrecedingFailure()
    {
        var speechClient = new ScriptedSpeechInputClient();
        for (var i = 0; i < 3; i++)
        {
            await speechClient.EnqueueRecordStepAsync(_ => throw new NoSpeechDetectedException());
        }

        var attempts = new List<FormVoiceCommandAttempt>();
        var observer = new Mock<IFormVoiceCommandAttemptObserver>();
        observer
            .Setup(o => o.OnAttemptStartingAsync(It.IsAny<FormVoiceCommandAttempt>(), It.IsAny<CancellationToken>()))
            .Callback<FormVoiceCommandAttempt, CancellationToken>((attempt, _) => attempts.Add(attempt))
            .Returns(Task.CompletedTask);

        var service = new FormVoiceCommandRecordingService(speechClient, speechClient, observer.Object);

        await Assert.ThrowsAsync<NoSpeechDetectedException>(() => service.RecordUntilSpeechAsync(
            SpeechCaptureMode.Listening,
            new VoiceDialogSettings { AllowBargeIn = true, MaxRetries = 2, NoInputTimeoutMs = 500 },
            CancellationToken.None));

        Assert.Equal(
            [
                new FormVoiceCommandAttempt(1, 3, FormVoiceCommandAttemptFailure.None),
                new FormVoiceCommandAttempt(2, 3, FormVoiceCommandAttemptFailure.NoInputTimeout),
                new FormVoiceCommandAttempt(3, 3, FormVoiceCommandAttemptFailure.NoInputTimeout),
            ],
            attempts);
    }

    [Fact]
    public async Task RecordSingleAttemptAsync_WhenAttemptContextInvalid_ThrowsBeforeFiringObserver()
    {
        var speechClient = new ScriptedSpeechInputClient();
        var observer = new Mock<IFormVoiceCommandAttemptObserver>(MockBehavior.Strict);
        var service = new FormVoiceCommandRecordingService(speechClient, speechClient, observer.Object);

        await Assert.ThrowsAsync<ArgumentException>(() => service.RecordSingleAttemptAsync(
            SpeechCaptureMode.Listening,
            CreateVoiceSettings(),
            startCursor: null,
            attempt: default,
            CancellationToken.None));

        Assert.Equal(0, speechClient.RecordCallCount);
        observer.VerifyNoOtherCalls();
    }

    [Fact]
    public void FormVoiceCommandAttempt_RejectsOutOfRangeContext()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FormVoiceCommandAttempt(0, 3, FormVoiceCommandAttemptFailure.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FormVoiceCommandAttempt(4, 3, FormVoiceCommandAttemptFailure.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FormVoiceCommandAttempt(1, 0, FormVoiceCommandAttemptFailure.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FormVoiceCommandAttempt(1, 3, (FormVoiceCommandAttemptFailure)99));
    }

    private static VoiceDialogSettings CreateVoiceSettings()
        => new()
        {
            AllowBargeIn = true,
            MaxRetries = 0,
            NoInputTimeoutMs = 500
        };
}
