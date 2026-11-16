using System.Buffers.Binary;
using Bunit;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Sprechen.Speech.App.ClientInfrastructure.Services;
using Sprechen.Speech.App.Contracts.Services;
using Sprechen.Speech.Runtime.Tests.TestDoubles;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class SpeechCaptureClientTests : TestContext
{
    // Der Testwert von 20 ms liegt unter den modellierten Mindestwerten; die Traces müssen den Wert des jeweiligen Modus zeigen.

    private const int SampleRateHz = 16000;
    private const int BytesPerSample = 2;

    [Fact]
    public async Task RecordAsync_WhenChunkStreamStalls_ReleasesSessionBeforeRetry()
    {
        var recorder = new ControlledMicrophoneRecorder();
        var settings = CreateCaptureSettings();
        var earconPlaybackState = CreateIdleEarconPlaybackState();
        var textToSpeechPlaybackState = CreateIdleTextToSpeechPlaybackState();
        var browserDebugConsole = new InMemoryBrowserDebugConsole();
        await browserDebugConsole.SetEnabledAsync(true, CancellationToken.None);
        await using var client = CreateClient(
            recorder,
            Mock.Of<IUserSpeechBargeInHandler>(),
            earconPlaybackState.Object,
            textToSpeechPlaybackState.Object,
            settings.Object,
            browserDebugConsole,
            new SpeechCaptureClientOptions
            {
                ChunkArrivalLivenessTimeout = TimeSpan.FromMilliseconds(50),
            });

        await Assert.ThrowsAsync<SpeechCaptureStreamStalledException>(() =>
            client.RecordAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(2)));
        Assert.Equal(1, recorder.ReleaseCallCount);

        using var retryCancellation = new CancellationTokenSource();
        var retry = client.RecordAsync(retryCancellation.Token);
        await WaitUntilAsync(
            () => browserDebugConsole.Entries.Count(entry => entry ==
                "speech-capture: capture armed mode=Listening startContext=CleanListening") == 2,
            TimeSpan.FromSeconds(2));
        Assert.Equal(2, recorder.StartCallCount);

        retryCancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            retry.WaitAsync(TimeSpan.FromSeconds(2)));
    }

    [Fact]
    public async Task DisposeAsync_WaitsForMicrophoneRelease()
    {
        var recordingStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var continueRelease = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var recorder = new ControlledMicrophoneRecorder(
            onReleaseInvoked: () => releaseEntered.TrySetResult(),
            beforeReleaseCompletesAsync: () => continueRelease.Task,
            afterStart: _ => recordingStarted.TrySetResult());
        var client = CreateClient(
            recorder,
            Mock.Of<IUserSpeechBargeInHandler>(),
            CreateIdleEarconPlaybackState().Object,
            CreateIdleTextToSpeechPlaybackState().Object,
            CreateCaptureSettings().Object,
            new NoOpBrowserDebugConsole());
        Task<CapturedSpeechAudio>? record = null;

        try
        {
            record = client.RecordAsync(CancellationToken.None);
            await recordingStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

            var dispose = client.DisposeAsync().AsTask();
            await releaseEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.False(dispose.IsCompleted);

            continueRelease.TrySetResult();
            await dispose.WaitAsync(TimeSpan.FromSeconds(2));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                record.WaitAsync(TimeSpan.FromSeconds(2)));
        }
        finally
        {
            continueRelease.TrySetResult();
            await client.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(2));
            if (record is not null)
            {
                try
                {
                    await record.WaitAsync(TimeSpan.FromSeconds(2));
                }
                catch (OperationCanceledException)
                {
                }
            }
        }
    }

    [Fact]
    public async Task DisposeAsync_AfterLifecycleShutdown_JoinsPendingSettingsCallback()
    {
        var settingsEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseSettings = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var settings = new Mock<ISpeechCaptureSilenceDebounceSettings>(MockBehavior.Strict);
        settings.Setup(owner => owner.GetSilenceDebounceMsAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                settingsEntered.TrySetResult();
                return new ValueTask<int>(releaseSettings.Task);
            });
        SpeechCaptureLocalSessionLifecycle? lifecycle = null;
        var client = CreateClient(
            new ControlledMicrophoneRecorder(),
            Mock.Of<IUserSpeechBargeInHandler>(),
            CreateIdleEarconPlaybackState().Object,
            CreateIdleTextToSpeechPlaybackState().Object,
            settings.Object,
            new NoOpBrowserDebugConsole(),
            new SpeechCaptureClientOptions { ShutdownTimeout = TimeSpan.FromSeconds(30) },
            onLifecycleCreated: created => lifecycle = created);
        Task<CapturedSpeechAudio>? record = null;

        try
        {
            record = client.RecordAsync(CancellationToken.None);
            await settingsEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));
            var shutdownOwner = lifecycle
                ?? throw new InvalidOperationException("The fixture did not expose its lifecycle.");
            await shutdownOwner.ShutdownAsync().WaitAsync(TimeSpan.FromSeconds(2));
            Assert.False(record.IsCompleted);

            // Der Lifecycle ist bereits beendet; hier muss Dispose noch auf den ausstehenden Capture warten.
            var dispose = client.DisposeAsync().AsTask();
            Assert.False(dispose.IsCompleted);

            releaseSettings.TrySetResult(20);
            await dispose.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.True(record.IsCompleted);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => record);
        }
        finally
        {
            releaseSettings.TrySetResult(20);
            await client.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(2));
            if (record is not null)
            {
                try
                {
                    await record.WaitAsync(TimeSpan.FromSeconds(2));
                }
                catch (OperationCanceledException)
                {
                }
            }
        }
    }

    [Fact]
    public async Task DisposeAsync_WhenSettingsCallbackDoesNotReturn_BoundsCaptureJoin()
    {
        var settingsEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseSettings = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var settings = new Mock<ISpeechCaptureSilenceDebounceSettings>(MockBehavior.Strict);
        settings.Setup(owner => owner.GetSilenceDebounceMsAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                settingsEntered.TrySetResult();
                return new ValueTask<int>(releaseSettings.Task);
            });
        var client = CreateClient(
            new ControlledMicrophoneRecorder(),
            Mock.Of<IUserSpeechBargeInHandler>(),
            CreateIdleEarconPlaybackState().Object,
            CreateIdleTextToSpeechPlaybackState().Object,
            settings.Object,
            new NoOpBrowserDebugConsole(),
            new SpeechCaptureClientOptions { ShutdownTimeout = TimeSpan.FromMilliseconds(100) });
        Task<CapturedSpeechAudio>? record = null;

        try
        {
            record = client.RecordAsync(CancellationToken.None);
            await settingsEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));

            await client.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(1));
            Assert.False(record.IsCompleted);

            releaseSettings.TrySetResult(20);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                record.WaitAsync(TimeSpan.FromSeconds(2)));
        }
        finally
        {
            releaseSettings.TrySetResult(20);
            await client.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(2));
            if (record is not null)
            {
                try
                {
                    await record.WaitAsync(TimeSpan.FromSeconds(2));
                }
                catch (OperationCanceledException)
                {
                }
            }
        }
    }

    [Fact]
    public async Task DisposeAsync_DuringPendingMicrophoneAccess_CancelsLifecycleBeforeWaiting()
    {
        var accessEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var recorder = new Mock<IMicrophoneRecorder>(MockBehavior.Strict);
        recorder
            .Setup(candidate => candidate.RequestAccessAsync(It.IsAny<CancellationToken>()))
            .Returns(async (CancellationToken token) =>
            {
                accessEntered.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, token).ConfigureAwait(false);
            });
        recorder
            .Setup(candidate => candidate.ReleaseAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var client = CreateClient(
            recorder.Object,
            Mock.Of<IUserSpeechBargeInHandler>(),
            CreateIdleEarconPlaybackState().Object,
            CreateIdleTextToSpeechPlaybackState().Object,
            CreateCaptureSettings().Object,
            new NoOpBrowserDebugConsole());

        var access = client.RequestMicrophoneAccessAsync(CancellationToken.None);
        await accessEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));

        await client.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(2));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => access);
        recorder.Verify(
            candidate => candidate.ReleaseAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task StopAsync_CancelsEveryCaptureAdmittedBeforeTheTerminalTransition()
    {
        var recorder = new ControlledMicrophoneRecorder();
        var secondSettingsEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseSecondSettings = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var settingsCallCount = 0;
        var settings = new Mock<ISpeechCaptureSilenceDebounceSettings>(MockBehavior.Strict);
        settings
            .Setup(service => service.GetSilenceDebounceMsAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken token) =>
            {
                if (Interlocked.Increment(ref settingsCallCount) == 1)
                {
                    return new ValueTask<int>(20);
                }

                secondSettingsEntered.TrySetResult();
                return new ValueTask<int>(releaseSecondSettings.Task.WaitAsync(token));
            });

        var bargeInHandler = new Mock<IUserSpeechBargeInHandler>(MockBehavior.Strict);
        var earconPlaybackState = new Mock<IEarconPlaybackState>(MockBehavior.Strict);
        earconPlaybackState.SetupGet(state => state.IsPlaying).Returns(false);
        earconPlaybackState
            .Setup(state => state.IsPlayingOrRecentlyCompleted(It.IsAny<TimeSpan>()))
            .Returns(false);
        var textToSpeechPlaybackState = new Mock<ITextToSpeechPlaybackState>(MockBehavior.Strict);
        textToSpeechPlaybackState.SetupGet(state => state.IsPlaying).Returns(false);
        textToSpeechPlaybackState
            .Setup(state => state.IsPlayingOrRecentlyCompleted(It.IsAny<TimeSpan>()))
            .Returns(false);
        var browserDebugConsole = new InMemoryBrowserDebugConsole();
        await browserDebugConsole.SetEnabledAsync(true, CancellationToken.None);

        await using var client = CreateClient(
            recorder,
            bargeInHandler.Object,
            earconPlaybackState.Object,
            textToSpeechPlaybackState.Object,
            settings.Object,
            browserDebugConsole);

        var activeCapture = client.RecordAsync(CancellationToken.None);
        await browserDebugConsole.WaitForEntryAsync(
            "speech-capture: capture armed mode=Listening startContext=CleanListening",
            TimeSpan.FromSeconds(1));

        var admittedCapture = client.RecordAsync(CancellationToken.None);
        await secondSettingsEntered.Task.WaitAsync(TimeSpan.FromSeconds(1));

        await client.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(2));
        releaseSecondSettings.TrySetResult(20);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => activeCapture.WaitAsync(TimeSpan.FromSeconds(2)));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => admittedCapture.WaitAsync(TimeSpan.FromSeconds(2)));
        Assert.Equal(1, recorder.StartCallCount);
        Assert.Equal(1, recorder.ReleaseCallCount);
    }

    [Fact]
    public async Task RecordAsync_WhenPreviousSessionFaulted_RestartsMicrophoneSession()
    {
        JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true).SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        JSInterop.Setup<string>("micRecorder.connect", _ => true).SetResult("audio/pcm;rate=16000");
        JSInterop.SetupVoid("micRecorder.disconnect", _ => true).SetVoidResult();

        await using var firstRecording = CreateInjectedRecording();
        await using var secondRecording = CreateInjectedRecording();

        var microphoneRecorder = new Mock<IMicrophoneRecorder>(MockBehavior.Strict);
        microphoneRecorder
            .Setup(recorder => recorder.RequestAccessAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        microphoneRecorder
            .SetupSequence(recorder => recorder.StartAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(firstRecording)
            .ReturnsAsync(secondRecording);

        var settings = new Mock<ISpeechCaptureSilenceDebounceSettings>(MockBehavior.Strict);
        settings
            .Setup(service => service.GetSilenceDebounceMsAsync(It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<int>(20));

        var bargeInHandler = new Mock<IUserSpeechBargeInHandler>(MockBehavior.Strict);
        bargeInHandler
            .Setup(handler => handler.OnSpeechCaptureStartingAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var earconPlaybackState = new Mock<IEarconPlaybackState>(MockBehavior.Strict);
        earconPlaybackState.SetupGet(state => state.IsPlaying).Returns(false);
        earconPlaybackState
            .Setup(state => state.IsPlayingOrRecentlyCompleted(It.IsAny<TimeSpan>()))
            .Returns(false);

        var textToSpeechPlaybackState = new Mock<ITextToSpeechPlaybackState>(MockBehavior.Strict);
        textToSpeechPlaybackState.SetupGet(state => state.IsPlaying).Returns(false);
        textToSpeechPlaybackState
            .Setup(state => state.IsPlayingOrRecentlyCompleted(It.IsAny<TimeSpan>()))
            .Returns(false);

        var browserDebugConsole = new InMemoryBrowserDebugConsole();
        await browserDebugConsole.SetEnabledAsync(true, CancellationToken.None);

        await using var client = CreateClient(
            microphoneRecorder.Object,
            bargeInHandler.Object,
            earconPlaybackState.Object,
            textToSpeechPlaybackState.Object,
            settings.Object,
            browserDebugConsole);

        var firstCaptureTask = client.RecordAsync(CancellationToken.None);
        firstRecording.OnError("boom");

        var firstException = await Assert.ThrowsAsync<InvalidOperationException>(() => firstCaptureTask);
        Assert.Equal("boom", firstException.Message);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var secondCaptureTask = client.RecordAsync(cts.Token);
        await browserDebugConsole.WaitForEntryAsync(
            "speech-capture: capture armed mode=Listening startContext=CleanListening",
            TimeSpan.FromSeconds(1),
            cts.Token);

        for (var i = 0; i < 6; i++)
        {
            secondRecording.OnChunk(CreateSpeechChunkMs(20));
        }

        FeedClosingSilence(secondRecording, SpeechCaptureMode.Listening);

        var utterance = await secondCaptureTask;

        microphoneRecorder.Verify(recorder => recorder.StartAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        Assert.Equal(SpeechAudioMimeType.Pcm, utterance.MimeType);
        Assert.NotEmpty(utterance.Chunks);
    }

    [Fact]
    public async Task RecordAsync_WhenTextToSpeechRecentlyCompleted_WaitsForPlaybackToSettleBeforeCapturing()
    {
        JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true).SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        JSInterop.Setup<string>("micRecorder.connect", _ => true).SetResult("audio/pcm;rate=16000");
        JSInterop.SetupVoid("micRecorder.disconnect", _ => true).SetVoidResult();

        await using var recording = CreateInjectedRecording();

        var microphoneRecorder = new Mock<IMicrophoneRecorder>(MockBehavior.Strict);
        microphoneRecorder
            .Setup(recorder => recorder.RequestAccessAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        microphoneRecorder
            .Setup(recorder => recorder.StartAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(recording);

        var settings = new Mock<ISpeechCaptureSilenceDebounceSettings>(MockBehavior.Strict);
        settings
            .Setup(service => service.GetSilenceDebounceMsAsync(It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<int>(20));

        var bargeInHandler = new Mock<IUserSpeechBargeInHandler>(MockBehavior.Strict);
        bargeInHandler
            .Setup(handler => handler.OnSpeechCaptureStartingAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var earconPlaybackState = new Mock<IEarconPlaybackState>(MockBehavior.Strict);
        earconPlaybackState.SetupGet(state => state.IsPlaying).Returns(false);
        earconPlaybackState
            .Setup(state => state.IsPlayingOrRecentlyCompleted(It.IsAny<TimeSpan>()))
            .Returns(false);

        var textToSpeechPlaybackState = new Mock<ITextToSpeechPlaybackState>(MockBehavior.Strict);
        textToSpeechPlaybackState.SetupGet(state => state.IsPlaying).Returns(false);
        textToSpeechPlaybackState
            .SetupSequence(state => state.IsPlayingOrRecentlyCompleted(It.IsAny<TimeSpan>()))
            .Returns(true)
            .Returns(true)
            .Returns(false)
            .Returns(false);

        var browserDebugConsole = new InMemoryBrowserDebugConsole();
        await browserDebugConsole.SetEnabledAsync(true, CancellationToken.None);

        await using var client = CreateClient(
            microphoneRecorder.Object,
            bargeInHandler.Object,
            earconPlaybackState.Object,
            textToSpeechPlaybackState.Object,
            settings.Object,
            browserDebugConsole);

        var recordTask = client.RecordAsync(CancellationToken.None);

        await browserDebugConsole.WaitForEntryAsync(
            "speech-capture: capture armed mode=Listening startContext=AssistantPlaybackRecentlySettledIdle",
            TimeSpan.FromSeconds(1));

        for (var i = 0; i < 14; i++)
        {
            recording.OnChunk(CreatePcmChunk(20, sampleValue: 1400));
        }

        FeedClosingSilence(recording, SpeechCaptureMode.Listening);

        var utterance = await recordTask;

        Assert.Equal(SpeechAudioMimeType.Pcm, utterance.MimeType);
        Assert.Contains(
            browserDebugConsole.Entries,
            entry => string.Equals(
                entry,
                "speech-capture: waiting for recent assistant playback to settle",
                StringComparison.Ordinal));
        Assert.Contains(
            browserDebugConsole.Entries,
            entry => string.Equals(
                entry,
                "speech-capture: recent assistant playback settled",
                StringComparison.Ordinal));
        Assert.Contains(
            browserDebugConsole.Entries,
            entry => string.Equals(
                entry,
                "speech-capture: record start mode=Listening silenceDebounceMs=500 startContext=AssistantPlaybackRecentlySettledIdle bargeInEnabled=False",
                StringComparison.Ordinal));
        textToSpeechPlaybackState.Verify(
            state => state.IsPlayingOrRecentlyCompleted(It.IsAny<TimeSpan>()),
            Times.AtLeast(3));
    }

    [Fact]
    public async Task RecordAsync_WhenInitialSilenceTimeoutOverrideIsProvided_UsesOverrideForNoSpeechDetection()
    {
        JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true).SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        JSInterop.Setup<string>("micRecorder.connect", _ => true).SetResult("audio/pcm;rate=16000");
        JSInterop.SetupVoid("micRecorder.disconnect", _ => true).SetVoidResult();

        await using var recording = CreateInjectedRecording();

        var microphoneRecorder = new Mock<IMicrophoneRecorder>(MockBehavior.Strict);
        microphoneRecorder
            .Setup(recorder => recorder.RequestAccessAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        microphoneRecorder
            .Setup(recorder => recorder.StartAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(recording);

        var settings = new Mock<ISpeechCaptureSilenceDebounceSettings>(MockBehavior.Strict);
        settings
            .Setup(service => service.GetSilenceDebounceMsAsync(It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<int>(20));

        var bargeInHandler = new Mock<IUserSpeechBargeInHandler>(MockBehavior.Strict);
        var earconPlaybackState = new Mock<IEarconPlaybackState>(MockBehavior.Strict);
        earconPlaybackState.SetupGet(state => state.IsPlaying).Returns(false);
        earconPlaybackState
            .Setup(state => state.IsPlayingOrRecentlyCompleted(It.IsAny<TimeSpan>()))
            .Returns(false);

        var textToSpeechPlaybackState = new Mock<ITextToSpeechPlaybackState>(MockBehavior.Strict);
        textToSpeechPlaybackState.SetupGet(state => state.IsPlaying).Returns(false);
        textToSpeechPlaybackState
            .Setup(state => state.IsPlayingOrRecentlyCompleted(It.IsAny<TimeSpan>()))
            .Returns(false);

        var browserDebugConsole = new InMemoryBrowserDebugConsole();
        await browserDebugConsole.SetEnabledAsync(true, CancellationToken.None);

        await using var client = CreateClient(
            microphoneRecorder.Object,
            bargeInHandler.Object,
            earconPlaybackState.Object,
            textToSpeechPlaybackState.Object,
            settings.Object,
            browserDebugConsole);

        var captureTask = ((IModeAwareSpeechCaptureClient)client).RecordAsync(
            SpeechCaptureMode.Listening,
            TimeSpan.FromMilliseconds(60),
            CancellationToken.None);

        await browserDebugConsole.WaitForEntryAsync(
            "speech-capture: capture armed mode=Listening startContext=CleanListening",
            TimeSpan.FromSeconds(1));

        recording.OnChunk(CreateSilenceChunkMs(20));
        recording.OnChunk(CreateSilenceChunkMs(20));
        recording.OnChunk(CreateSilenceChunkMs(20));
        recording.OnChunk(CreateSilenceChunkMs(20));

        await Assert.ThrowsAsync<NoSpeechDetectedException>(() => captureTask);
    }

    [Fact]
    public async Task RecordAsync_WhenListeningModeDetectsSpeech_DoesNotTriggerBargeInHandler()
    {
        JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true).SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        JSInterop.Setup<string>("micRecorder.connect", _ => true).SetResult("audio/pcm;rate=16000");
        JSInterop.SetupVoid("micRecorder.disconnect", _ => true).SetVoidResult();

        await using var recording = CreateInjectedRecording();

        var microphoneRecorder = new Mock<IMicrophoneRecorder>(MockBehavior.Strict);
        microphoneRecorder
            .Setup(recorder => recorder.RequestAccessAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        microphoneRecorder
            .Setup(recorder => recorder.StartAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(recording);

        var settings = new Mock<ISpeechCaptureSilenceDebounceSettings>(MockBehavior.Strict);
        settings
            .Setup(service => service.GetSilenceDebounceMsAsync(It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<int>(20));

        var bargeInHandler = new Mock<IUserSpeechBargeInHandler>(MockBehavior.Strict);

        var earconPlaybackState = new Mock<IEarconPlaybackState>(MockBehavior.Strict);
        earconPlaybackState.SetupGet(state => state.IsPlaying).Returns(false);
        earconPlaybackState
            .Setup(state => state.IsPlayingOrRecentlyCompleted(It.IsAny<TimeSpan>()))
            .Returns(false);

        var textToSpeechPlaybackState = new Mock<ITextToSpeechPlaybackState>(MockBehavior.Strict);
        textToSpeechPlaybackState.SetupGet(state => state.IsPlaying).Returns(false);
        textToSpeechPlaybackState
            .Setup(state => state.IsPlayingOrRecentlyCompleted(It.IsAny<TimeSpan>()))
            .Returns(false);

        var browserDebugConsole = new InMemoryBrowserDebugConsole();
        await browserDebugConsole.SetEnabledAsync(true, CancellationToken.None);

        await using var client = CreateClient(
            microphoneRecorder.Object,
            bargeInHandler.Object,
            earconPlaybackState.Object,
            textToSpeechPlaybackState.Object,
            settings.Object,
            browserDebugConsole);

        var recordTask = client.RecordAsync(CancellationToken.None);
        await browserDebugConsole.WaitForEntryAsync(
            "speech-capture: capture armed mode=Listening startContext=CleanListening",
            TimeSpan.FromSeconds(1));

        for (var i = 0; i < 6; i++)
        {
            recording.OnChunk(CreateSpeechChunkMs(20));
        }

        FeedClosingSilence(recording, SpeechCaptureMode.Listening);

        var utterance = await recordTask;

        Assert.Equal(SpeechAudioMimeType.Pcm, utterance.MimeType);
        bargeInHandler.Verify(
            handler => handler.OnSpeechCaptureStartingAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RecordAsync_WhenPromptInterruptionModeDetectsSpeech_TriggersBargeInHandlerOnce()
    {
        JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true).SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        JSInterop.Setup<string>("micRecorder.connect", _ => true).SetResult("audio/pcm;rate=16000");
        JSInterop.SetupVoid("micRecorder.disconnect", _ => true).SetVoidResult();

        await using var recording = CreateInjectedRecording();

        var microphoneRecorder = new Mock<IMicrophoneRecorder>(MockBehavior.Strict);
        microphoneRecorder
            .Setup(recorder => recorder.RequestAccessAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        microphoneRecorder
            .Setup(recorder => recorder.StartAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(recording);

        var settings = new Mock<ISpeechCaptureSilenceDebounceSettings>(MockBehavior.Strict);
        settings
            .Setup(service => service.GetSilenceDebounceMsAsync(It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<int>(20));

        var bargeInHandler = new Mock<IUserSpeechBargeInHandler>(MockBehavior.Strict);
        bargeInHandler
            .Setup(handler => handler.OnSpeechCaptureStartingAsync(It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var earconPlaybackState = new Mock<IEarconPlaybackState>(MockBehavior.Strict);
        earconPlaybackState.SetupGet(state => state.IsPlaying).Returns(false);
        earconPlaybackState
            .Setup(state => state.IsPlayingOrRecentlyCompleted(It.IsAny<TimeSpan>()))
            .Returns(false);

        var textToSpeechPlaybackState = new Mock<ITextToSpeechPlaybackState>(MockBehavior.Strict);
        textToSpeechPlaybackState.SetupGet(state => state.IsPlaying).Returns(true);
        textToSpeechPlaybackState
            .Setup(state => state.IsPlayingOrRecentlyCompleted(It.IsAny<TimeSpan>()))
            .Returns(true);

        var browserDebugConsole = new InMemoryBrowserDebugConsole();
        await browserDebugConsole.SetEnabledAsync(true, CancellationToken.None);

        await using var client = CreateClient(
            microphoneRecorder.Object,
            bargeInHandler.Object,
            earconPlaybackState.Object,
            textToSpeechPlaybackState.Object,
            settings.Object,
            browserDebugConsole);

        var recordTask = ((IModeAwareSpeechCaptureClient)client).RecordAsync(
            SpeechCaptureMode.PromptInterruption,
            CancellationToken.None);

        await browserDebugConsole.WaitForEntryAsync(
            "speech-capture: capture armed mode=PromptInterruption startContext=AssistantPlaybackActive",
            TimeSpan.FromSeconds(1));

        for (var i = 0; i < 8; i++)
        {
            recording.OnChunk(CreatePcmChunk(20, sampleValue: 600));
        }

        for (var i = 0; i < 6; i++)
        {
            recording.OnChunk(CreateSpeechChunkMs(20));
        }

        FeedClosingSilence(recording, SpeechCaptureMode.PromptInterruption);

        var utterance = await recordTask;

        Assert.Equal(SpeechAudioMimeType.Pcm, utterance.MimeType);
        Assert.Contains(
            browserDebugConsole.Entries,
            entry => string.Equals(
                entry,
                "speech-capture: record start mode=PromptInterruption silenceDebounceMs=500 startContext=AssistantPlaybackActive bargeInEnabled=True",
                StringComparison.Ordinal));
        bargeInHandler.Verify(
            handler => handler.OnSpeechCaptureStartingAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RecordAsync_WhenPromptInterruptionModeOnlySeesLeakageSpike_DoesNotTriggerBargeInHandler()
    {
        JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true).SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        JSInterop.Setup<string>("micRecorder.connect", _ => true).SetResult("audio/pcm;rate=16000");
        JSInterop.SetupVoid("micRecorder.disconnect", _ => true).SetVoidResult();

        await using var recording = CreateInjectedRecording();

        var microphoneRecorder = new Mock<IMicrophoneRecorder>(MockBehavior.Strict);
        microphoneRecorder
            .Setup(recorder => recorder.RequestAccessAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        microphoneRecorder
            .Setup(recorder => recorder.StartAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(recording);

        var settings = new Mock<ISpeechCaptureSilenceDebounceSettings>(MockBehavior.Strict);
        settings
            .Setup(service => service.GetSilenceDebounceMsAsync(It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<int>(20));

        var bargeInHandler = new Mock<IUserSpeechBargeInHandler>(MockBehavior.Strict);

        var earconPlaybackState = new Mock<IEarconPlaybackState>(MockBehavior.Strict);
        earconPlaybackState.SetupGet(state => state.IsPlaying).Returns(false);
        earconPlaybackState
            .Setup(state => state.IsPlayingOrRecentlyCompleted(It.IsAny<TimeSpan>()))
            .Returns(false);

        var textToSpeechPlaybackState = new Mock<ITextToSpeechPlaybackState>(MockBehavior.Strict);
        textToSpeechPlaybackState.SetupGet(state => state.IsPlaying).Returns(true);
        textToSpeechPlaybackState
            .Setup(state => state.IsPlayingOrRecentlyCompleted(It.IsAny<TimeSpan>()))
            .Returns(true);

        var browserDebugConsole = new InMemoryBrowserDebugConsole();
        await browserDebugConsole.SetEnabledAsync(true, CancellationToken.None);

        await using var client = CreateClient(
            microphoneRecorder.Object,
            bargeInHandler.Object,
            earconPlaybackState.Object,
            textToSpeechPlaybackState.Object,
            settings.Object,
            browserDebugConsole);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var recordTask = ((IModeAwareSpeechCaptureClient)client).RecordAsync(
            SpeechCaptureMode.PromptInterruption,
            cts.Token);

        await browserDebugConsole.WaitForEntryAsync(
            "speech-capture: capture armed mode=PromptInterruption startContext=AssistantPlaybackActive",
            TimeSpan.FromSeconds(1),
            cts.Token);

        for (var i = 0; i < 8; i++)
        {
            recording.OnChunk(CreatePcmChunk(20, sampleValue: 600));
        }

        recording.OnChunk(CreatePcmChunk(20, sampleValue: 1100));
        recording.OnChunk(CreatePcmChunk(20, sampleValue: 600));
        recording.OnChunk(CreatePcmChunk(20, sampleValue: 600));

        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => recordTask);
        Assert.DoesNotContain(
            browserDebugConsole.Entries,
            entry => string.Equals(
                entry,
                "speech-capture: speech onset confirmed; requesting prompt interruption",
                StringComparison.Ordinal));
        bargeInHandler.Verify(
            handler => handler.OnSpeechCaptureStartingAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RecordAsync_WhenPromptInterruptionModeOnlySeesRisingPromptLeakage_DoesNotTriggerBargeInHandler()
    {
        JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true).SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        JSInterop.Setup<string>("micRecorder.connect", _ => true).SetResult("audio/pcm;rate=16000");
        JSInterop.SetupVoid("micRecorder.disconnect", _ => true).SetVoidResult();

        await using var recording = CreateInjectedRecording();

        var microphoneRecorder = new Mock<IMicrophoneRecorder>(MockBehavior.Strict);
        microphoneRecorder
            .Setup(recorder => recorder.RequestAccessAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        microphoneRecorder
            .Setup(recorder => recorder.StartAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(recording);

        var settings = new Mock<ISpeechCaptureSilenceDebounceSettings>(MockBehavior.Strict);
        settings
            .Setup(service => service.GetSilenceDebounceMsAsync(It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<int>(20));

        var bargeInHandler = new Mock<IUserSpeechBargeInHandler>(MockBehavior.Strict);

        var earconPlaybackState = new Mock<IEarconPlaybackState>(MockBehavior.Strict);
        earconPlaybackState.SetupGet(state => state.IsPlaying).Returns(false);
        earconPlaybackState
            .Setup(state => state.IsPlayingOrRecentlyCompleted(It.IsAny<TimeSpan>()))
            .Returns(false);

        var textToSpeechPlaybackState = new Mock<ITextToSpeechPlaybackState>(MockBehavior.Strict);
        textToSpeechPlaybackState.SetupGet(state => state.IsPlaying).Returns(true);
        textToSpeechPlaybackState
            .Setup(state => state.IsPlayingOrRecentlyCompleted(It.IsAny<TimeSpan>()))
            .Returns(true);

        var browserDebugConsole = new InMemoryBrowserDebugConsole();
        await browserDebugConsole.SetEnabledAsync(true, CancellationToken.None);

        await using var client = CreateClient(
            microphoneRecorder.Object,
            bargeInHandler.Object,
            earconPlaybackState.Object,
            textToSpeechPlaybackState.Object,
            settings.Object,
            browserDebugConsole);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var recordTask = ((IModeAwareSpeechCaptureClient)client).RecordAsync(
            SpeechCaptureMode.PromptInterruption,
            cts.Token);

        await browserDebugConsole.WaitForEntryAsync(
            "speech-capture: capture armed mode=PromptInterruption startContext=AssistantPlaybackActive",
            TimeSpan.FromSeconds(1),
            cts.Token);

        for (var i = 0; i < 8; i++)
        {
            var sampleValue = (short)(500 + (i * 100));
            recording.OnChunk(CreatePcmChunk(20, sampleValue));
        }

        recording.OnChunk(CreatePcmChunk(20, sampleValue: 1200));

        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => recordTask);
        Assert.DoesNotContain(
            browserDebugConsole.Entries,
            entry => string.Equals(
                entry,
                "speech-capture: speech onset confirmed; requesting prompt interruption",
                StringComparison.Ordinal));
        bargeInHandler.Verify(
            handler => handler.OnSpeechCaptureStartingAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RecordAsync_WhenPromptInterruptionDetectionModeDetectsSpeech_DoesNotTriggerBargeInHandler()
    {
        JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true).SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        JSInterop.Setup<string>("micRecorder.connect", _ => true).SetResult("audio/pcm;rate=16000");
        JSInterop.SetupVoid("micRecorder.disconnect", _ => true).SetVoidResult();

        await using var recording = CreateInjectedRecording();

        var microphoneRecorder = new Mock<IMicrophoneRecorder>(MockBehavior.Strict);
        microphoneRecorder
            .Setup(recorder => recorder.RequestAccessAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        microphoneRecorder
            .Setup(recorder => recorder.StartAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(recording);

        var settings = new Mock<ISpeechCaptureSilenceDebounceSettings>(MockBehavior.Strict);
        settings
            .Setup(service => service.GetSilenceDebounceMsAsync(It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<int>(20));

        var bargeInHandler = new Mock<IUserSpeechBargeInHandler>(MockBehavior.Strict);

        var earconPlaybackState = new Mock<IEarconPlaybackState>(MockBehavior.Strict);
        earconPlaybackState.SetupGet(state => state.IsPlaying).Returns(false);
        earconPlaybackState
            .Setup(state => state.IsPlayingOrRecentlyCompleted(It.IsAny<TimeSpan>()))
            .Returns(false);

        var textToSpeechPlaybackState = new Mock<ITextToSpeechPlaybackState>(MockBehavior.Strict);
        textToSpeechPlaybackState.SetupGet(state => state.IsPlaying).Returns(true);
        textToSpeechPlaybackState
            .Setup(state => state.IsPlayingOrRecentlyCompleted(It.IsAny<TimeSpan>()))
            .Returns(true);

        var browserDebugConsole = new InMemoryBrowserDebugConsole();
        await browserDebugConsole.SetEnabledAsync(true, CancellationToken.None);

        await using var client = CreateClient(
            microphoneRecorder.Object,
            bargeInHandler.Object,
            earconPlaybackState.Object,
            textToSpeechPlaybackState.Object,
            settings.Object,
            browserDebugConsole);

        var recordTask = ((IModeAwareSpeechCaptureClient)client).RecordAsync(
            SpeechCaptureMode.PromptInterruptionDetection,
            CancellationToken.None);

        await browserDebugConsole.WaitForEntryAsync(
            "speech-capture: capture armed mode=PromptInterruptionDetection startContext=AssistantPlaybackActive",
            TimeSpan.FromSeconds(1));

        for (var i = 0; i < 8; i++)
        {
            recording.OnChunk(CreatePcmChunk(20, sampleValue: 600));
        }

        for (var i = 0; i < 6; i++)
        {
            recording.OnChunk(CreateSpeechChunkMs(20));
        }

        FeedClosingSilence(recording, SpeechCaptureMode.PromptInterruptionDetection);

        var utterance = await recordTask;

        Assert.Equal(SpeechAudioMimeType.Pcm, utterance.MimeType);
        Assert.Contains(
            browserDebugConsole.Entries,
            entry => string.Equals(
                entry,
                "speech-capture: record start mode=PromptInterruptionDetection silenceDebounceMs=500 startContext=AssistantPlaybackActive bargeInEnabled=False",
                StringComparison.Ordinal));
        Assert.Contains(
            browserDebugConsole.Entries,
            entry => string.Equals(
                entry,
                "speech-capture: speech onset confirmed for prompt interruption detection",
                StringComparison.Ordinal));
        bargeInHandler.Verify(
            handler => handler.OnSpeechCaptureStartingAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RecordAsync_WhenActivationListeningModeIsUsed_AppliesWakeWordCaptureProfile()
    {
        JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true).SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        JSInterop.Setup<string>("micRecorder.connect", _ => true).SetResult("audio/pcm;rate=16000");
        JSInterop.SetupVoid("micRecorder.disconnect", _ => true).SetVoidResult();

        await using var recording = CreateInjectedRecording();

        var microphoneRecorder = new Mock<IMicrophoneRecorder>(MockBehavior.Strict);
        microphoneRecorder
            .Setup(recorder => recorder.RequestAccessAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        microphoneRecorder
            .Setup(recorder => recorder.StartAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(recording);

        var settings = new Mock<ISpeechCaptureSilenceDebounceSettings>(MockBehavior.Strict);
        settings
            .Setup(service => service.GetSilenceDebounceMsAsync(It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<int>(20));

        var bargeInHandler = new Mock<IUserSpeechBargeInHandler>(MockBehavior.Strict);

        var earconPlaybackState = new Mock<IEarconPlaybackState>(MockBehavior.Strict);
        earconPlaybackState.SetupGet(state => state.IsPlaying).Returns(false);
        earconPlaybackState
            .Setup(state => state.IsPlayingOrRecentlyCompleted(It.IsAny<TimeSpan>()))
            .Returns(false);

        var textToSpeechPlaybackState = new Mock<ITextToSpeechPlaybackState>(MockBehavior.Strict);
        textToSpeechPlaybackState.SetupGet(state => state.IsPlaying).Returns(false);
        textToSpeechPlaybackState
            .Setup(state => state.IsPlayingOrRecentlyCompleted(It.IsAny<TimeSpan>()))
            .Returns(false);

        var browserDebugConsole = new InMemoryBrowserDebugConsole();
        await browserDebugConsole.SetEnabledAsync(true, CancellationToken.None);

        var captureOptions = new SpeechCaptureClientOptions
        {

        };
        var segmenterOptions = new PcmSpeechCaptureSegmenterOptions
        {
            SpeechStartConfirmation = TimeSpan.FromMilliseconds(120),
            MaxUtteranceDuration = TimeSpan.FromSeconds(5)
        };
        await using var client = CreateClient(
            microphoneRecorder.Object,
            bargeInHandler.Object,
            earconPlaybackState.Object,
            textToSpeechPlaybackState.Object,
            settings.Object,
            browserDebugConsole,
            captureOptions,
            segmenterOptions);

        var recordTask = ((IModeAwareSpeechCaptureClient)client).RecordAsync(
            SpeechCaptureMode.ActivationListening,
            CancellationToken.None);

        await browserDebugConsole.WaitForEntryAsync(
            "speech-capture: capture armed mode=ActivationListening startContext=CleanListening",
            TimeSpan.FromSeconds(1));

        for (var i = 0; i < 9; i++)
        {
            recording.OnChunk(CreateSpeechChunkMs(20));
        }

        Assert.False(recordTask.IsCompleted);

        recording.OnChunk(CreateSpeechChunkMs(20));
        for (var i = 0; i < 60; i++)
        {
            recording.OnChunk(CreateSilenceChunkMs(20));
        }

        var utterance = await recordTask;

        Assert.Equal(SpeechAudioMimeType.Pcm, utterance.MimeType);
        Assert.True(utterance.Chunks.Count >= 10);
        Assert.Contains(
            browserDebugConsole.Entries,
            entry => string.Equals(
                entry,
                "speech-capture: record start mode=ActivationListening silenceDebounceMs=900 startContext=CleanListening bargeInEnabled=False",
                StringComparison.Ordinal));
    }

    [Fact]
    public async Task RecordAsync_WhenActivationListeningContinuationModeIsUsed_AppliesContinuationCaptureProfile()
    {
        JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true).SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        JSInterop.Setup<string>("micRecorder.connect", _ => true).SetResult("audio/pcm;rate=16000");
        JSInterop.SetupVoid("micRecorder.disconnect", _ => true).SetVoidResult();

        await using var recording = CreateInjectedRecording();

        var microphoneRecorder = new Mock<IMicrophoneRecorder>(MockBehavior.Strict);
        microphoneRecorder
            .Setup(recorder => recorder.RequestAccessAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        microphoneRecorder
            .Setup(recorder => recorder.StartAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(recording);

        var settings = new Mock<ISpeechCaptureSilenceDebounceSettings>(MockBehavior.Strict);
        settings
            .Setup(service => service.GetSilenceDebounceMsAsync(It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<int>(20));

        var bargeInHandler = new Mock<IUserSpeechBargeInHandler>(MockBehavior.Strict);

        var earconPlaybackState = new Mock<IEarconPlaybackState>(MockBehavior.Strict);
        earconPlaybackState.SetupGet(state => state.IsPlaying).Returns(false);
        earconPlaybackState
            .Setup(state => state.IsPlayingOrRecentlyCompleted(It.IsAny<TimeSpan>()))
            .Returns(false);

        var textToSpeechPlaybackState = new Mock<ITextToSpeechPlaybackState>(MockBehavior.Strict);
        textToSpeechPlaybackState.SetupGet(state => state.IsPlaying).Returns(false);
        textToSpeechPlaybackState
            .Setup(state => state.IsPlayingOrRecentlyCompleted(It.IsAny<TimeSpan>()))
            .Returns(false);

        var browserDebugConsole = new InMemoryBrowserDebugConsole();
        await browserDebugConsole.SetEnabledAsync(true, CancellationToken.None);

        var captureOptions = new SpeechCaptureClientOptions
        {

        };
        var segmenterOptions = new PcmSpeechCaptureSegmenterOptions
        {
            SpeechStartConfirmation = TimeSpan.FromMilliseconds(120),
            MaxUtteranceDuration = TimeSpan.FromSeconds(5)
        };
        await using var client = CreateClient(
            microphoneRecorder.Object,
            bargeInHandler.Object,
            earconPlaybackState.Object,
            textToSpeechPlaybackState.Object,
            settings.Object,
            browserDebugConsole,
            captureOptions,
            segmenterOptions);

        var recordTask = ((IModeAwareSpeechCaptureClient)client).RecordAsync(
            SpeechCaptureMode.ActivationListeningContinuation,
            CancellationToken.None);

        await browserDebugConsole.WaitForEntryAsync(
            "speech-capture: capture armed mode=ActivationListeningContinuation startContext=CleanListening",
            TimeSpan.FromSeconds(1));

        for (var i = 0; i < 15; i++)
        {
            recording.OnChunk(CreateSpeechChunkMs(20));
        }

        Assert.False(recordTask.IsCompleted);

        recording.OnChunk(CreateSpeechChunkMs(20));
        for (var i = 0; i < 60; i++)
        {
            recording.OnChunk(CreateSilenceChunkMs(20));
        }

        var utterance = await recordTask;

        Assert.Equal(SpeechAudioMimeType.Pcm, utterance.MimeType);
        Assert.True(utterance.Chunks.Count >= 16);
        Assert.Contains(
            browserDebugConsole.Entries,
            entry => string.Equals(
                entry,
                "speech-capture: record start mode=ActivationListeningContinuation silenceDebounceMs=500 startContext=CleanListening bargeInEnabled=False",
                StringComparison.Ordinal));
    }

    private static byte[] CreateSpeechChunkMs(int durationMs)
        => CreatePcmChunk(durationMs, sampleValue: 1000);

    private static byte[] CreateSilenceChunkMs(int durationMs)
        => CreatePcmChunk(durationMs, sampleValue: 0);

    /// <summary>
    /// Speist für diese Testkonfiguration Stille über den modellierten Mindestwert hinaus ein.
    /// Die zusätzlichen 100 ms vermeiden einen Abschluss genau an der Grenze.
    /// </summary>
    private static void FeedClosingSilence(ScriptedMicrophoneRecording recording, SpeechCaptureMode mode)
    {
        var required = (SpeechCaptureProfileCatalogReader.GetSilenceDebounceMs(mode) ?? 0) + 100;
        for (var fed = 0; fed < required; fed += 20)
        {
            recording.OnChunk(CreateSilenceChunkMs(20));
        }
    }

    private static ScriptedMicrophoneRecording CreateInjectedRecording()
    {
        var recording = ScriptedMicrophoneRecording.Create(static (candidate, _) =>
        {
            candidate.OnStopped();
            return Task.CompletedTask;
        });
        recording.SetMimeType("audio/pcm;rate=16000");
        return recording;
    }

    private static byte[] CreatePcmChunk(int durationMs, short sampleValue)
    {
        var samples = durationMs * SampleRateHz / 1000;
        var buffer = new byte[samples * BytesPerSample];

        for (var i = 0; i < samples; i++)
        {
            var offset = i * BytesPerSample;
            BinaryPrimitives.WriteInt16LittleEndian(buffer.AsSpan(offset, BytesPerSample), sampleValue);
        }

        return buffer;
    }

    private static Mock<ISpeechCaptureSilenceDebounceSettings> CreateCaptureSettings()
    {
        var settings = new Mock<ISpeechCaptureSilenceDebounceSettings>(MockBehavior.Strict);
        settings
            .Setup(service => service.GetSilenceDebounceMsAsync(It.IsAny<CancellationToken>()))
            .Returns(new ValueTask<int>(20));
        return settings;
    }

    private static Mock<IEarconPlaybackState> CreateIdleEarconPlaybackState()
    {
        var state = new Mock<IEarconPlaybackState>(MockBehavior.Strict);
        state.SetupGet(candidate => candidate.IsPlaying).Returns(false);
        state.Setup(candidate => candidate.IsPlayingOrRecentlyCompleted(It.IsAny<TimeSpan>())).Returns(false);
        return state;
    }

    private static Mock<ITextToSpeechPlaybackState> CreateIdleTextToSpeechPlaybackState()
    {
        var state = new Mock<ITextToSpeechPlaybackState>(MockBehavior.Strict);
        state.SetupGet(candidate => candidate.IsPlaying).Returns(false);
        state.Setup(candidate => candidate.IsPlayingOrRecentlyCompleted(It.IsAny<TimeSpan>())).Returns(false);
        return state;
    }

    private static async Task WaitUntilAsync(Func<bool> predicate, TimeSpan timeout)
    {
        using var cancellation = new CancellationTokenSource(timeout);
        while (!predicate())
        {
            await Task.Delay(TimeSpan.FromMilliseconds(10), cancellation.Token);
        }
    }

    private static SpeechCaptureClient CreateClient(
        IMicrophoneRecorder microphoneRecorder,
        IUserSpeechBargeInHandler bargeInHandler,
        IEarconPlaybackState earconPlaybackState,
        ITextToSpeechPlaybackState textToSpeechPlaybackState,
        ISpeechCaptureSilenceDebounceSettings speechCaptureSettings,
        IBrowserDebugConsole browserDebugConsole,
        SpeechCaptureClientOptions? captureOptions = null,
        PcmSpeechCaptureSegmenterOptions? segmenterOptions = null,
        Action<SpeechCaptureLocalSessionLifecycle>? onLifecycleCreated = null)
    {
        var effectiveCaptureOptions = captureOptions ?? new SpeechCaptureClientOptions();
        var effectiveSegmenterOptions = segmenterOptions ?? new PcmSpeechCaptureSegmenterOptions();

        // Beide Koordinatoren teilen denselben Ring, damit der erfasste Cursor auf die später gelesenen Daten zeigt.
        var ring = new InMemorySpeechSampleRing(maxLookbackMs: 5_000);
        var coordinator = new AssistantPlaybackCoordinator(
            earconPlaybackState,
            textToSpeechPlaybackState,
            Options.Create(effectiveCaptureOptions),
            ring,
            browserDebugConsole,
            NullLogger<AssistantPlaybackCoordinator>.Instance);
        var silenceDebounceResolver = new SpeechCaptureSilenceDebounceResolver(
            speechCaptureSettings,
            NullLogger<SpeechCaptureSilenceDebounceResolver>.Instance);
        var optionsFactory = new SpeechCaptureSegmenterOptionsFactory(
            Options.Create(effectiveCaptureOptions),
            Options.Create(effectiveSegmenterOptions));
        var factory = new SpeechCaptureSegmenterFactory(optionsFactory);
        var sessionCoordinator = new SpeechCaptureSessionCoordinator(
            microphoneRecorder,
            ring,
            new SpeechAudioMimeTypeResolver(),
            NullLogger<SpeechCaptureSessionCoordinator>.Instance);
        var activeCaptureCoordinator = new SpeechCaptureActiveCaptureCoordinator(
            bargeInHandler,
            NullLogger<SpeechCaptureActiveCaptureCoordinator>.Instance,
            browserDebugConsole,
            factory,
            ring,
            TestMicGateInfrastructure.NoOpState(),
            captureOptions: Options.Create(effectiveCaptureOptions));
        var dispatcher = new SpeechCaptureChunkDispatcher(activeCaptureCoordinator);
        var lifecycle = new SpeechCaptureLocalSessionLifecycle(
            sessionCoordinator,
            dispatcher,
            Options.Create(effectiveCaptureOptions));
        onLifecycleCreated?.Invoke(lifecycle);
        return new SpeechCaptureClient(
            coordinator,
            silenceDebounceResolver,
            lifecycle,
            activeCaptureCoordinator,
            Options.Create(effectiveCaptureOptions));
    }
}
