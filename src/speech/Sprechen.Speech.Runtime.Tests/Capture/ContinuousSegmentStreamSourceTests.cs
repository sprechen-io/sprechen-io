using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Sprechen.Speech.Runtime.Tests.Services;
using Sprechen.Speech.Runtime.Tests.TestDoubles;

namespace Sprechen.Speech.Runtime.Tests.Capture;

public sealed class ContinuousSegmentStreamSourceTests
{
    private const int SampleRateHz = InMemorySpeechSampleRing.SampleRateHz;
    private const int BytesPerSample = InMemorySpeechSampleRing.BytesPerSample;

    [Fact]
    public async Task CaptureNextSegmentAsync_PreservesBufferedSpeechThatArrivedBeforeSegmentArming()
    {
        var recorder = new ControlledMicrophoneRecorder();
        var (source, lifecycle) = CreateSubject(recorder, TimeSpan.FromSeconds(1));

        await using var cleanup = new AsyncCleanup(() => lifecycle.DisposeAsync());
        await using var sourceCleanup = new AsyncCleanup(() => source.DisposeAsync());

        await source.BeginListeningAsync(SpeechCaptureMode.ActivationListening, CancellationToken.None);

        recorder.PushChunk(MakeSpeech(200));
        await Task.Delay(50);

        var captureTask = source.CaptureNextSegmentAsync(CancellationToken.None);

        // Die Aktivierung verwendet mindestens die modellierte Stillefrist.
        // Das Signal enthält längere Abschlussstille, damit hier ein fertiges Segment entsteht.
        for (var i = 0; i < 60; i++)
        {
            recorder.PushChunk(MakeSilence(20));
        }

        var captured = await captureTask.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.NotEmpty(captured.Chunks);
        Assert.True(captured.Chunks.Sum(chunk => chunk.Length) >= MakeSpeech(200).Length);
        Assert.Contains(captured.Chunks.SelectMany(chunk => chunk), sample => sample != 0);
        Assert.Equal(1, recorder.StartCallCount);

        await source.StopListeningAsync(CancellationToken.None);
    }

    [Fact]
    public async Task CaptureNextSegmentAsync_EmitsStableModeAwareTraceAfterContinuousSlotIsArmed()
    {
        var recorder = new ControlledMicrophoneRecorder();
        var browserDebugConsole = new InMemoryBrowserDebugConsole();
        await browserDebugConsole.SetEnabledAsync(true, CancellationToken.None);
        var (source, lifecycle) = CreateSubject(
            recorder,
            TimeSpan.FromSeconds(1),
            browserDebugConsole);

        await using var cleanup = new AsyncCleanup(() => lifecycle.DisposeAsync());
        await using var sourceCleanup = new AsyncCleanup(() => source.DisposeAsync());

        await source.BeginListeningAsync(SpeechCaptureMode.ActivationListening, CancellationToken.None);

        var captureTask = source.CaptureNextSegmentAsync(CancellationToken.None);
        const string startTrace =
            "speech-capture: record start mode=ActivationListening silenceDebounceMs=900 startContext=CleanListening bargeInEnabled=False";
        const string armedTrace =
            "speech-capture: capture armed mode=ActivationListening startContext=CleanListening";
        await browserDebugConsole.WaitForEntryAsync(armedTrace, TimeSpan.FromSeconds(1));

        for (var i = 0; i < 10; i++)
        {
            recorder.PushChunk(MakeSpeech(20));
        }

        for (var i = 0; i < 60; i++)
        {
            recorder.PushChunk(MakeSilence(20));
        }

        await captureTask.WaitAsync(TimeSpan.FromSeconds(5));

        var entries = browserDebugConsole.Entries.ToArray();
        var startIndex = Array.IndexOf(entries, startTrace);
        var armedIndex = Array.IndexOf(entries, armedTrace);
        var completeIndex = Array.FindIndex(
            entries,
            entry => entry.StartsWith("speech-capture: record complete ", StringComparison.Ordinal));
        Assert.True(startIndex >= 0, $"Missing stable mode-aware start trace: {startTrace}");
        Assert.True(armedIndex > startIndex, "The armed trace must follow the start trace.");
        Assert.True(completeIndex > armedIndex, "The structured completion trace must follow the armed trace.");
    }

    [Fact]
    public async Task DisposeAsync_ReleasesListeningLeaseAndAllowsIdleShutdown()
    {
        var recorder = new ControlledMicrophoneRecorder();
        var (source, lifecycle) = CreateSubject(recorder, TimeSpan.FromMilliseconds(100));

        await using var cleanup = new AsyncCleanup(() => lifecycle.DisposeAsync());

        await source.BeginListeningAsync(SpeechCaptureMode.ActivationListening, CancellationToken.None);

        await source.DisposeAsync();

        for (var attempt = 0; attempt < 100 && recorder.StopCallCount == 0; attempt++)
        {
            await Task.Delay(25);
        }

        Assert.Equal(1, recorder.StartCallCount);
        Assert.True(
            recorder.StopCallCount > 0,
            "Disposing the wake-listening segment source should release its listening lease so the local session can idle-stop.");
    }

    [Fact]
    public async Task DisposeAsync_WhenBeginListeningIsQueued_RejectsQueuedBeginWithoutLeakingItsLease()
    {
        var requestEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var continueRequest = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var recording = new Mock<IMicrophoneRecording>(MockBehavior.Strict);
        recording.SetupGet(candidate => candidate.MimeType).Returns("audio/pcm");
        recording.SetupGet(candidate => candidate.Chunks).Returns(EmptyChunks());
        recording.SetupGet(candidate => candidate.Completion).Returns(Task.CompletedTask);
        recording.Setup(candidate => candidate.DisposeAsync()).Returns(ValueTask.CompletedTask);
        var recorder = new Mock<IMicrophoneRecorder>(MockBehavior.Strict);
        recorder
            .Setup(candidate => candidate.RequestAccessAsync(It.IsAny<CancellationToken>()))
            .Returns(async (CancellationToken token) =>
            {
                requestEntered.TrySetResult();
                await continueRequest.Task.WaitAsync(token).ConfigureAwait(false);
            });
        recorder
            .Setup(candidate => candidate.StartAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(recording.Object);
        recorder
            .Setup(candidate => candidate.ReleaseAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var (source, lifecycle) = CreateSubject(recorder.Object, TimeSpan.FromMilliseconds(50));

        await using var cleanup = new AsyncCleanup(() => lifecycle.DisposeAsync());
        var firstBegin = source.BeginListeningAsync(
            SpeechCaptureMode.ActivationListening,
            CancellationToken.None).AsTask();
        await requestEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));

        var queuedCapture = source.CaptureNextSegmentAsync(CancellationToken.None);
        var dispose = source.DisposeAsync().AsTask();
        var queuedBegin = source.BeginListeningAsync(
            SpeechCaptureMode.ActivationListening,
            CancellationToken.None).AsTask();

        await dispose.WaitAsync(TimeSpan.FromSeconds(2));
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            firstBegin.WaitAsync(TimeSpan.FromSeconds(2)));
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            queuedBegin.WaitAsync(TimeSpan.FromSeconds(2)));
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            queuedCapture.WaitAsync(TimeSpan.FromSeconds(2)));
        recorder.Verify(candidate => candidate.StartAsync(It.IsAny<CancellationToken>()), Times.Never);
        recorder.Verify(candidate => candidate.ReleaseAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CaptureNextSegmentAsync_WhenSessionEndedBetweenCaptures_ReacquiresBeforeRetry()
    {
        var recorder = new ControlledMicrophoneRecorder();
        var (source, lifecycle) = CreateSubject(recorder, TimeSpan.FromSeconds(30));

        await using var cleanup = new AsyncCleanup(() => lifecycle.DisposeAsync());
        await using var sourceCleanup = new AsyncCleanup(() => source.DisposeAsync());

        await source.BeginListeningAsync(SpeechCaptureMode.ActivationListening, CancellationToken.None);
        recorder.CompleteRecording();

        var capture = source.CaptureNextSegmentAsync(CancellationToken.None);
        await WaitUntilAsync(() => recorder.StartCallCount == 2, TimeSpan.FromSeconds(2));

        for (var i = 0; i < 10; i++)
        {
            recorder.PushChunk(MakeSpeech(20));
        }

        for (var i = 0; i < 60; i++)
        {
            recorder.PushChunk(MakeSilence(20));
        }

        var captured = await capture.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.NotEmpty(captured.Chunks);
        Assert.Equal(2, recorder.StartCallCount);
    }

    [Fact]
    public async Task CaptureNextSegmentAsync_WhenChunkStreamStalls_ReleasesSessionBeforeRetry()
    {
        var recorder = new ControlledMicrophoneRecorder();
        var (source, lifecycle) = CreateSubject(
            recorder,
            TimeSpan.FromSeconds(30),
            chunkLivenessTimeout: TimeSpan.FromMilliseconds(50));

        await using var cleanup = new AsyncCleanup(() => lifecycle.DisposeAsync());
        await using var sourceCleanup = new AsyncCleanup(() => source.DisposeAsync());

        await source.BeginListeningAsync(SpeechCaptureMode.ActivationListening, CancellationToken.None);

        await Assert.ThrowsAsync<SpeechCaptureStreamStalledException>(() =>
            source.CaptureNextSegmentAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(2)));
        Assert.Equal(1, recorder.ReleaseCallCount);

        using var retryCancellation = new CancellationTokenSource();
        var retry = source.CaptureNextSegmentAsync(retryCancellation.Token);
        await WaitUntilAsync(() => recorder.StartCallCount == 2, TimeSpan.FromSeconds(2));

        retryCancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            retry.WaitAsync(TimeSpan.FromSeconds(2)));
    }

    [Fact]
    public async Task CaptureNextSegmentAsync_WhenReplacementImmediatelyEmitsAudio_PreservesItsFirstChunks()
    {
        ControlledMicrophoneRecorder? recorder = null;
        recorder = new ControlledMicrophoneRecorder(afterStart: startCall =>
        {
            if (startCall != 2)
            {
                return;
            }

            for (var index = 0; index < 10; index++)
            {
                recorder!.PushChunk(MakeSpeech(20));
            }

            for (var index = 0; index < 60; index++)
            {
                recorder!.PushChunk(MakeSilence(20));
            }
        });
        var (source, lifecycle) = CreateSubject(recorder, TimeSpan.FromSeconds(30));

        await using var cleanup = new AsyncCleanup(() => lifecycle.DisposeAsync());
        await using var sourceCleanup = new AsyncCleanup(() => source.DisposeAsync());

        await source.BeginListeningAsync(SpeechCaptureMode.ActivationListening, CancellationToken.None);
        recorder.CompleteRecording();

        var captured = await source.CaptureNextSegmentAsync(CancellationToken.None)
            .WaitAsync(TimeSpan.FromSeconds(5));

        Assert.NotEmpty(captured.Chunks);
        Assert.Contains(captured.Chunks.SelectMany(chunk => chunk), sample => sample != 0);
        Assert.Equal(2, recorder.StartCallCount);
    }

    [Fact]
    public async Task DisposeAsync_WhenReplacementStartIsBlocked_CancelsCaptureAndDoesNotHang()
    {
        var replacementEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var recorder = new ControlledMicrophoneRecorder(
            beforeStartCompletesAsync: async (startCall, token) =>
            {
                if (startCall == 2)
                {
                    replacementEntered.TrySetResult();
                    await Task.Delay(Timeout.InfiniteTimeSpan, token).ConfigureAwait(false);
                }
            });
        var (source, lifecycle) = CreateSubject(recorder, TimeSpan.FromSeconds(30));

        await using var cleanup = new AsyncCleanup(() => lifecycle.DisposeAsync());
        await source.BeginListeningAsync(SpeechCaptureMode.ActivationListening, CancellationToken.None);
        recorder.CompleteRecording();

        var capture = source.CaptureNextSegmentAsync(CancellationToken.None);
        await replacementEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var dispose = source.DisposeAsync().AsTask();

        await dispose.WaitAsync(TimeSpan.FromSeconds(2));
        await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            capture.WaitAsync(TimeSpan.FromSeconds(2)));
    }

    [Fact]
    public async Task DisposeAsync_JoinsArmedCaptureBeforeCoordinatorDependencyIsDisposed()
    {
        var recorder = new ControlledMicrophoneRecorder();
        var browserDebugConsole = new InMemoryBrowserDebugConsole();
        await browserDebugConsole.SetEnabledAsync(true, CancellationToken.None);
        var (source, lifecycle, coordinator) = CreateSubjectWithCoordinator(
            recorder,
            TimeSpan.FromSeconds(30),
            browserDebugConsole);

        await using var cleanup = new AsyncCleanup(() => lifecycle.DisposeAsync());
        await source.BeginListeningAsync(SpeechCaptureMode.ActivationListening, CancellationToken.None);
        var capture = source.CaptureNextSegmentAsync(CancellationToken.None);
        await browserDebugConsole.WaitForEntryAsync(
            "speech-capture: capture armed mode=ActivationListening startContext=CleanListening",
            TimeSpan.FromSeconds(2));

        await source.DisposeAsync();
        coordinator.Dispose();

        var exception = await Assert.ThrowsAsync<ObjectDisposedException>(() =>
            capture.WaitAsync(TimeSpan.FromSeconds(2)));
        Assert.Equal(nameof(ContinuousSegmentStreamSource), exception.ObjectName);
    }

    [Fact]
    public async Task DisposeAsync_ConcurrentCallersAwaitTheSameCleanup()
    {
        var settingStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var settingResult = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var settings = new Mock<ISpeechCaptureSilenceDebounceSettings>(MockBehavior.Strict);
        settings.Setup(owner => owner.GetSilenceDebounceMsAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                settingStarted.TrySetResult();
                return new ValueTask<int>(settingResult.Task);
            });
        var (source, lifecycle, coordinator) = CreateSubjectWithCoordinator(
            new ControlledMicrophoneRecorder(),
            new SpeechCaptureClientOptions { LocalSessionIdleTimeout = TimeSpan.FromSeconds(30) },
            captureSettings: settings.Object);

        try
        {
            await source.BeginListeningAsync(SpeechCaptureMode.ActivationListening, CancellationToken.None);
            var capture = source.CaptureNextSegmentAsync(CancellationToken.None);
            await settingStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

            var firstDispose = source.DisposeAsync().AsTask();
            var secondDispose = source.DisposeAsync().AsTask();

            Assert.Same(firstDispose, secondDispose);
            Assert.False(firstDispose.IsCompleted);
            Assert.False(secondDispose.IsCompleted);

            settingResult.TrySetResult(80);
            await Task.WhenAll(firstDispose, secondDispose).WaitAsync(TimeSpan.FromSeconds(2));
            await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                capture.WaitAsync(TimeSpan.FromSeconds(2)));
        }
        finally
        {
            settingResult.TrySetResult(80);
            await source.DisposeAsync();
            await lifecycle.DisposeAsync();
            coordinator.Dispose();
        }
    }

    [Fact]
    public async Task DisposeAsync_WhenCaptureCallbackNeverReturns_ReleasesScopeAfterShutdownBound()
    {
        var settingStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var settingResult = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var settings = new Mock<ISpeechCaptureSilenceDebounceSettings>(MockBehavior.Strict);
        settings.Setup(owner => owner.GetSilenceDebounceMsAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                settingStarted.TrySetResult();
                return new ValueTask<int>(settingResult.Task);
            });
        var captureOptions = new SpeechCaptureClientOptions
        {
            LocalSessionIdleTimeout = TimeSpan.FromSeconds(30),
            ShutdownTimeout = TimeSpan.FromMilliseconds(100),
        };
        var (source, lifecycle, coordinator) = CreateSubjectWithCoordinator(
            new ControlledMicrophoneRecorder(),
            captureOptions,
            captureSettings: settings.Object);

        try
        {
            await source.BeginListeningAsync(SpeechCaptureMode.ActivationListening, CancellationToken.None);
            var capture = source.CaptureNextSegmentAsync(CancellationToken.None);
            await settingStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

            await source.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(1));
            Assert.False(capture.IsCompleted);

            settingResult.TrySetResult(80);
            var exception = await Assert.ThrowsAsync<ObjectDisposedException>(() =>
                capture.WaitAsync(TimeSpan.FromSeconds(2)));
            Assert.Equal(nameof(ContinuousSegmentStreamSource), exception.ObjectName);
        }
        finally
        {
            settingResult.TrySetResult(80);
            await source.DisposeAsync();
            await lifecycle.DisposeAsync();
            coordinator.Dispose();
        }
    }

    [Fact]
    public async Task DisposeAsync_WhenRecorderReleaseNeverReturns_ReleasesScopeAfterShutdownBound()
    {
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var recorder = new ControlledMicrophoneRecorder(
            beforeReleaseCompletesAsync: () => release.Task);
        var captureOptions = new SpeechCaptureClientOptions
        {
            LocalSessionIdleTimeout = TimeSpan.FromSeconds(30),
            ShutdownTimeout = TimeSpan.FromMilliseconds(100),
        };
        var (source, lifecycle, coordinator) = CreateSubjectWithCoordinator(
            recorder,
            captureOptions);

        try
        {
            await source.BeginListeningAsync(SpeechCaptureMode.ActivationListening, CancellationToken.None);
            var stop = lifecycle.StopAsync(CancellationToken.None);
            await WaitUntilAsync(
                () => recorder.ReleaseCallCount == 1,
                TimeSpan.FromSeconds(2));

            await source.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(1));

            Assert.Equal(1, recorder.ReleaseCallCount);
            Assert.False(stop.IsCompleted);

            release.TrySetResult();
            await stop.WaitAsync(TimeSpan.FromSeconds(2));
        }
        finally
        {
            release.TrySetResult();
            await lifecycle.DisposeAsync();
            coordinator.Dispose();
        }
    }

    [Fact]
    public async Task BeginListeningAsync_WhenTheMicrophoneCannotBeAcquired_CarriesTheAcquisitionFailureAsItsCause()
    {
        // Die innere Ursache muss erhalten bleiben, damit ein fehlendes Gerät
        // eine passende Rückmeldung statt einer allgemeinen technischen Fehlermeldung erhält.
        var recorder = new Mock<IMicrophoneRecorder>(MockBehavior.Strict);
        recorder
            .Setup(candidate => candidate.RequestAccessAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new MicrophoneAcquisitionException(MicrophoneAcquisitionFailureCode.DeviceNotFound));
        recorder
            .Setup(candidate => candidate.ReleaseAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var (source, lifecycle) = CreateSubject(recorder.Object, TimeSpan.FromSeconds(30));

        await using var cleanup = new AsyncCleanup(() => lifecycle.DisposeAsync());
        await using var sourceCleanup = new AsyncCleanup(() => source.DisposeAsync());

        var exception = await Assert.ThrowsAsync<ContinuousSegmentStreamUnavailableException>(() =>
            source.BeginListeningAsync(SpeechCaptureMode.ActivationListening, CancellationToken.None).AsTask());

        var cause = FindAcquisitionCause(exception);
        Assert.NotNull(cause);
        Assert.Equal(MicrophoneAcquisitionFailureCode.DeviceNotFound, cause.Code);
        recorder.Verify(candidate => candidate.StartAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CaptureNextSegmentAsync_WhenReacquisitionFailsAfterTheSessionEnded_CarriesTheAcquisitionFailureAsItsCause()
    {
        var recorder = new ControlledMicrophoneRecorder(
            beforeStartCompletesAsync: (startCall, _) => startCall == 2
                ? Task.FromException(new MicrophoneAcquisitionException(MicrophoneAcquisitionFailureCode.PermissionDenied))
                : Task.CompletedTask);
        var (source, lifecycle) = CreateSubject(recorder, TimeSpan.FromSeconds(30));

        await using var cleanup = new AsyncCleanup(() => lifecycle.DisposeAsync());
        await using var sourceCleanup = new AsyncCleanup(() => source.DisposeAsync());

        await source.BeginListeningAsync(SpeechCaptureMode.ActivationListening, CancellationToken.None);
        recorder.CompleteRecording();

        var exception = await Assert.ThrowsAsync<ContinuousSegmentStreamUnavailableException>(() =>
            source.CaptureNextSegmentAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5)));

        var cause = FindAcquisitionCause(exception);
        Assert.NotNull(cause);
        Assert.Equal(MicrophoneAcquisitionFailureCode.PermissionDenied, cause.Code);
        Assert.Equal(2, recorder.StartCallCount);
    }

    private static MicrophoneAcquisitionException? FindAcquisitionCause(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is MicrophoneAcquisitionException acquisition)
            {
                return acquisition;
            }
        }

        return null;
    }

    private static (ContinuousSegmentStreamSource Source, SpeechCaptureLocalSessionLifecycle Lifecycle) CreateSubject(
        IMicrophoneRecorder recorder,
        TimeSpan idleTimeout,
        IBrowserDebugConsole? browserDebugConsole = null,
        TimeSpan? chunkLivenessTimeout = null)
    {
        var subject = CreateSubjectWithCoordinator(
            recorder,
            new SpeechCaptureClientOptions
            {
                LocalSessionIdleTimeout = idleTimeout,
                ChunkArrivalLivenessTimeout = chunkLivenessTimeout ?? TimeSpan.FromSeconds(3),
            },
            browserDebugConsole,
            chunkLivenessTimeout);
        return (subject.Source, subject.Lifecycle);
    }

    private static (
        ContinuousSegmentStreamSource Source,
        SpeechCaptureLocalSessionLifecycle Lifecycle,
        ActiveSegmentCoordinator Coordinator) CreateSubjectWithCoordinator(
        IMicrophoneRecorder recorder,
        TimeSpan idleTimeout,
        IBrowserDebugConsole? browserDebugConsole = null,
        TimeSpan? chunkLivenessTimeout = null)
        => CreateSubjectWithCoordinator(
            recorder,
            new SpeechCaptureClientOptions
            {
                LocalSessionIdleTimeout = idleTimeout,
                ChunkArrivalLivenessTimeout = chunkLivenessTimeout ?? TimeSpan.FromSeconds(3),
            },
            browserDebugConsole,
            chunkLivenessTimeout);

    private static (
        ContinuousSegmentStreamSource Source,
        SpeechCaptureLocalSessionLifecycle Lifecycle,
        ActiveSegmentCoordinator Coordinator) CreateSubjectWithCoordinator(
        IMicrophoneRecorder recorder,
        SpeechCaptureClientOptions captureOptions,
        IBrowserDebugConsole? browserDebugConsole = null,
        TimeSpan? chunkLivenessTimeout = null,
        ISpeechCaptureSilenceDebounceSettings? captureSettings = null)
    {
        browserDebugConsole ??= new NoOpBrowserDebugConsole();
        var ring = new InMemorySpeechSampleRing(maxLookbackMs: 5_000);

        var segmenterFactory = new SpeechCaptureSegmenterFactory(
            new SpeechCaptureSegmenterOptionsFactory(
                Options.Create(captureOptions),
                Options.Create(new PcmSpeechCaptureSegmenterOptions
                {
                    SpeechStartConfirmation = TimeSpan.FromMilliseconds(40),
                    TailSilence = TimeSpan.FromMilliseconds(80),
                    MaxInitialSilence = TimeSpan.FromSeconds(5),
                })));

        var activeCaptureCoordinator = new SpeechCaptureActiveCaptureCoordinator(
            new NoOpUserSpeechBargeInHandler(),
            NullLogger<SpeechCaptureActiveCaptureCoordinator>.Instance,
            browserDebugConsole,
            segmenterFactory,
            ring,
            new StubMicGateState(isHardOff: false));
        var dispatcher = new SpeechCaptureChunkDispatcher(activeCaptureCoordinator);
        var sessionCoordinator = new SpeechCaptureSessionCoordinator(
            recorder,
            ring,
            new SpeechAudioMimeTypeResolver(),
            NullLogger<SpeechCaptureSessionCoordinator>.Instance);
        var lifecycle = new SpeechCaptureLocalSessionLifecycle(
            sessionCoordinator,
            dispatcher,
            Options.Create(captureOptions));

        var settings = new Mock<ISpeechCaptureSilenceDebounceSettings>(MockBehavior.Strict);
        settings
            .Setup(owner => owner.GetSilenceDebounceMsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(80);

        var activeSegmentCoordinator = new ActiveSegmentCoordinator(
            segmenterFactory,
            ring,
            browserDebugConsole,
            Options.Create(captureOptions));

        var source = new ContinuousSegmentStreamSource(
            lifecycle,
            dispatcher,
            new AssistantPlaybackCoordinator(
                new RecordingEarconPlaybackState(),
                new RecordingPlaybackState(),
                Options.Create(captureOptions),
                ring,
                browserDebugConsole,
                NullLogger<AssistantPlaybackCoordinator>.Instance),
            new SpeechCaptureSilenceDebounceResolver(
                captureSettings ?? settings.Object,
                NullLogger<SpeechCaptureSilenceDebounceResolver>.Instance),
            activeSegmentCoordinator,
            Options.Create(captureOptions));

        return (source, lifecycle, activeSegmentCoordinator);
    }

    private static byte[] MakeSpeech(int ms)
    {
        var bytes = new byte[ms * SampleRateHz * BytesPerSample / 1000];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = 0x55;
        }

        return bytes;
    }

    private static byte[] MakeSilence(int ms) => new byte[ms * SampleRateHz * BytesPerSample / 1000];

    private static async IAsyncEnumerable<byte[]> EmptyChunks()
    {
        await Task.CompletedTask;
        yield break;
    }

    private static async Task WaitUntilAsync(Func<bool> predicate, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        while (!predicate())
        {
            await Task.Delay(TimeSpan.FromMilliseconds(10), cts.Token).ConfigureAwait(false);
        }
    }
}
