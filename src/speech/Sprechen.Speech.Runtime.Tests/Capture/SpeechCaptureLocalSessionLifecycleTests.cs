using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Sprechen.Speech.Runtime.Tests.Services;
using Sprechen.Speech.Runtime.Tests.TestDoubles;

namespace Sprechen.Speech.Runtime.Tests.Capture;

public sealed class SpeechCaptureLocalSessionLifecycleTests
{
    [Fact]
    public async Task AcquireAsync_ReusesWarmSessionBeforeIdleTimeout_AndStopsAfterTimeout()
    {
        var recorder = new ControlledMicrophoneRecorder();
        await using var lifecycle = CreateLifecycle(recorder, TimeSpan.FromMilliseconds(250));

        await using (await lifecycle.AcquireAsync(CancellationToken.None))
        {
        }

        await Task.Delay(20);
        Assert.Equal(1, recorder.StartCallCount);
        Assert.Equal(0, recorder.StopCallCount);

        await using (await lifecycle.AcquireAsync(CancellationToken.None))
        {
        }

        Assert.Equal(1, recorder.StartCallCount);
        await WaitUntilAsync(() => recorder.StopCallCount == 1, TimeSpan.FromSeconds(5));
        Assert.Equal(1, recorder.RequestAccessCallCount);
    }

    [Fact]
    public async Task PauseAsync_ReleasesMicrophoneSoAnnouncedOffMeansOff()
    {
        var recorder = new ControlledMicrophoneRecorder();
        await using var lifecycle = CreateLifecycle(recorder, TimeSpan.FromSeconds(30));

        await using (await lifecycle.AcquireAsync(CancellationToken.None))
        {
        }

        Assert.Equal(1, recorder.StartCallCount);
        Assert.Equal(0, recorder.ReleaseCallCount);

        await lifecycle.PauseAsync(CancellationToken.None);

        Assert.Equal(1, recorder.ReleaseCallCount);

        await using (await lifecycle.AcquireAsync(CancellationToken.None))
        {
        }

        Assert.Equal(2, recorder.StartCallCount);
    }

    [Fact]
    public async Task StopAsync_ReleasesMicrophoneSoStoppedMeansOff()
    {
        var recorder = new ControlledMicrophoneRecorder();
        await using var lifecycle = CreateLifecycle(recorder, TimeSpan.FromSeconds(30));

        await using (await lifecycle.AcquireAsync(CancellationToken.None))
        {
        }

        Assert.Equal(1, recorder.StartCallCount);
        Assert.Equal(0, recorder.ReleaseCallCount);

        await lifecycle.StopAsync(CancellationToken.None);

        Assert.Equal(1, recorder.ReleaseCallCount);
    }

    [Fact]
    public async Task StopAsync_WhenChunkCallbackNeverReturns_ReleasesMicrophoneWhileShutdownFenceIsRetained()
    {
        var recorder = new ControlledMicrophoneRecorder();
        var ring = new InMemorySpeechSampleRing(maxLookbackMs: 5_000);
        var captureOptions = new SpeechCaptureClientOptions
        {
            LocalSessionIdleTimeout = TimeSpan.FromSeconds(30),
        };
        using var activeCaptureCoordinator = new SpeechCaptureActiveCaptureCoordinator(
            new NoOpUserSpeechBargeInHandler(),
            NullLogger<SpeechCaptureActiveCaptureCoordinator>.Instance,
            new NoOpBrowserDebugConsole(),
            CreateSegmenterFactory(captureOptions),
            ring,
            new StubMicGateState(isHardOff: false));
        var dispatcher = new SpeechCaptureChunkDispatcher(activeCaptureCoordinator);
        using var sessionCoordinator = new SpeechCaptureSessionCoordinator(
            recorder,
            ring,
            new SpeechAudioMimeTypeResolver(),
            NullLogger<SpeechCaptureSessionCoordinator>.Instance);
        var lifecycle = new SpeechCaptureLocalSessionLifecycle(
            sessionCoordinator,
            dispatcher,
            Options.Create(captureOptions));
        var callbackEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseCallback = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        try
        {
            await sessionCoordinator.RequestMicrophoneAccessAsync(CancellationToken.None);
            await sessionCoordinator.EnsureSessionStartedAsync(
                async (_, _) =>
                {
                    callbackEntered.TrySetResult();
                    await releaseCallback.Task.ConfigureAwait(false);
                },
                static _ => { },
                CancellationToken.None);
            recorder.PushChunk([1, 2]);
            await callbackEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));

            using var callerCancellation = new CancellationTokenSource();
            var stop = lifecycle.StopAsync(callerCancellation.Token);
            await WaitUntilAsync(() => recorder.StopCallCount == 1, TimeSpan.FromSeconds(2));
            callerCancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => stop);

            await WaitUntilAsync(() => recorder.ReleaseCallCount == 1, TimeSpan.FromSeconds(2));
            Assert.False(releaseCallback.Task.IsCompleted);
        }
        finally
        {
            releaseCallback.TrySetResult();
            await lifecycle.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(2));
            await lifecycle.DisposeAsync();
        }
    }

    [Fact]
    public async Task StopAsync_WhenReleaseFailsAndChunkCallbackIsBlocked_ReportsFailureBeforeCallbackFenceAndRemainsRetryable()
    {
        var callbackEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseCallback = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var recording = new MicrophoneRecording((candidate, _) =>
        {
            candidate.OnStopped();
            return Task.CompletedTask;
        });
        recording.SetMimeType("audio/pcm");
        var releaseCallCount = 0;
        var recorder = new Mock<IMicrophoneRecorder>(MockBehavior.Strict);
        recorder
            .Setup(candidate => candidate.RequestAccessAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        recorder
            .Setup(candidate => candidate.StartAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(recording);
        recorder
            .Setup(candidate => candidate.ReleaseAsync(It.IsAny<CancellationToken>()))
            .Returns(() => Interlocked.Increment(ref releaseCallCount) == 1
                ? Task.FromException(new InvalidOperationException("release failed"))
                : Task.CompletedTask);
        var ring = new InMemorySpeechSampleRing(maxLookbackMs: 5_000);
        var captureOptions = new SpeechCaptureClientOptions
        {
            LocalSessionIdleTimeout = TimeSpan.FromSeconds(30),
        };
        using var activeCaptureCoordinator = new SpeechCaptureActiveCaptureCoordinator(
            new NoOpUserSpeechBargeInHandler(),
            NullLogger<SpeechCaptureActiveCaptureCoordinator>.Instance,
            new NoOpBrowserDebugConsole(),
            CreateSegmenterFactory(captureOptions),
            ring,
            new StubMicGateState(isHardOff: false));
        using var sessionCoordinator = new SpeechCaptureSessionCoordinator(
            recorder.Object,
            ring,
            new SpeechAudioMimeTypeResolver(),
            NullLogger<SpeechCaptureSessionCoordinator>.Instance);
        var lifecycle = new SpeechCaptureLocalSessionLifecycle(
            sessionCoordinator,
            new SpeechCaptureChunkDispatcher(activeCaptureCoordinator),
            Options.Create(captureOptions));
        Task? firstStop = null;
        Task? retry = null;

        try
        {
            await sessionCoordinator.RequestMicrophoneAccessAsync(CancellationToken.None);
            await sessionCoordinator.EnsureSessionStartedAsync(
                async (_, _) =>
                {
                    callbackEntered.TrySetResult();
                    await releaseCallback.Task.ConfigureAwait(false);
                },
                static _ => { },
                CancellationToken.None);
            recording.OnChunk([1, 2]);
            await callbackEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));

            firstStop = lifecycle.StopAsync(CancellationToken.None);
            await WaitUntilAsync(() => Volatile.Read(ref releaseCallCount) == 1, TimeSpan.FromSeconds(2));
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                firstStop.WaitAsync(TimeSpan.FromMilliseconds(500)));

            retry = lifecycle.StopAsync(CancellationToken.None);
            await WaitUntilAsync(() => Volatile.Read(ref releaseCallCount) == 2, TimeSpan.FromSeconds(2));
            Assert.False(retry.IsCompleted);

            releaseCallback.TrySetResult();
            await retry.WaitAsync(TimeSpan.FromSeconds(2));
        }
        finally
        {
            releaseCallback.TrySetResult();
            if (firstStop is not null)
            {
                try
                {
                    await firstStop.WaitAsync(TimeSpan.FromSeconds(2));
                }
                catch (InvalidOperationException)
                {
                }
            }

            if (retry is not null)
            {
                await retry.WaitAsync(TimeSpan.FromSeconds(2));
            }

            await lifecycle.DisposeAsync();
        }
    }

    [Fact]
    public async Task StopAsync_BeforeCaptureArms_ReleasesMicrophoneAndCancelsPendingCapture()
    {
        var recorder = new ControlledMicrophoneRecorder();
        var gateEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseGate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var micGate = new Mock<IMicGateState>(MockBehavior.Strict);
        micGate.Setup(state => state.WaitUntilOpenAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken _) =>
            {
                gateEntered.TrySetResult();
                return new ValueTask(releaseGate.Task);
            });
        var ring = new InMemorySpeechSampleRing(maxLookbackMs: 5_000);
        var captureOptions = new SpeechCaptureClientOptions
        {
            LocalSessionIdleTimeout = TimeSpan.FromSeconds(30),
        };
        var segmenterFactory = CreateSegmenterFactory(captureOptions);
        using var activeCaptureCoordinator = new SpeechCaptureActiveCaptureCoordinator(
            new NoOpUserSpeechBargeInHandler(),
            NullLogger<SpeechCaptureActiveCaptureCoordinator>.Instance,
            new NoOpBrowserDebugConsole(),
            segmenterFactory,
            ring,
            micGate.Object);
        var dispatcher = new SpeechCaptureChunkDispatcher(activeCaptureCoordinator);
        using var sessionCoordinator = new SpeechCaptureSessionCoordinator(
            recorder,
            ring,
            new SpeechAudioMimeTypeResolver(),
            NullLogger<SpeechCaptureSessionCoordinator>.Instance);
        await using var lifecycle = new SpeechCaptureLocalSessionLifecycle(
            sessionCoordinator,
            dispatcher,
            Options.Create(captureOptions));
        await using var lease = await lifecycle.AcquireAsync(CancellationToken.None);

        Task<CapturedSpeechAudio>? capture = null;

        try
        {
            capture = activeCaptureCoordinator.CaptureAsync(
                SpeechAudioMimeType.Pcm,
                silenceDebounceMs: 200,
                SpeechCaptureStartContext.CleanListening,
                SpeechCaptureModeBehavior.For(SpeechCaptureMode.Listening),
                initialSilenceTimeout: TimeSpan.FromSeconds(30),
                CancellationToken.None);
            await gateEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));

            await lifecycle.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(2));
            Assert.Equal(1, recorder.ReleaseCallCount);
            Assert.False(capture.IsCompleted);

            releaseGate.TrySetResult();
            await Assert.ThrowsAsync<TaskCanceledException>(() => capture.WaitAsync(TimeSpan.FromSeconds(2)));
        }
        finally
        {
            releaseGate.TrySetResult();
            await lifecycle.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(2));
            if (capture is not null)
            {
                try
                {
                    await capture.WaitAsync(TimeSpan.FromSeconds(2));
                }
                catch (OperationCanceledException)
                {
                }
            }
        }
    }

    [Fact]
    public async Task IdleStop_ReleasesMicrophoneSoStoppedMeansOff()
    {
        var recorder = new ControlledMicrophoneRecorder();
        await using var lifecycle = CreateLifecycle(recorder, TimeSpan.FromMilliseconds(250));

        await using (await lifecycle.AcquireAsync(CancellationToken.None))
        {
        }

        await WaitUntilAsync(() => recorder.ReleaseCallCount == 1, TimeSpan.FromSeconds(5));
        Assert.Equal(1, recorder.StopCallCount);
    }

    [Fact]
    public async Task StopAsync_WithCancelledCaller_StillStartsTerminalRelease()
    {
        var stopEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var continueStop = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var recorder = new ControlledMicrophoneRecorder(beforeStopCompletesAsync: async () =>
        {
            stopEntered.TrySetResult();
            await continueStop.Task.ConfigureAwait(false);
        });
        await using var lifecycle = CreateLifecycle(recorder, TimeSpan.FromSeconds(30));
        await using var lease = await lifecycle.AcquireAsync(CancellationToken.None);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var stop = lifecycle.StopAsync(cancellation.Token);
        await stopEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        try
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => stop);
        }
        finally
        {
            continueStop.TrySetResult();
        }

        await WaitUntilAsync(() => recorder.ReleaseCallCount == 1, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task StopAsync_WhenBrowserDisconnectCancels_StillCompletesTerminalRelease()
    {
        var recording = new Mock<IMicrophoneRecording>(MockBehavior.Strict);
        recording.SetupGet(candidate => candidate.Completion).Returns(Task.CompletedTask);
        recording.SetupGet(candidate => candidate.MimeType).Returns("audio/pcm");
        recording.SetupGet(candidate => candidate.Chunks).Returns(EmptyChunks());
        recording
            .Setup(candidate => candidate.DisposeAsync())
            .Returns(ValueTask.FromException(new OperationCanceledException("browser disconnected")));
        var recorder = new Mock<IMicrophoneRecorder>(MockBehavior.Strict);
        recorder
            .Setup(candidate => candidate.RequestAccessAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        recorder
            .Setup(candidate => candidate.StartAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(recording.Object);
        recorder
            .Setup(candidate => candidate.ReleaseAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        await using var lifecycle = CreateLifecycle(recorder.Object, TimeSpan.FromSeconds(30));

        await using (await lifecycle.AcquireAsync(CancellationToken.None))
        {
        }

        await lifecycle.StopAsync(CancellationToken.None);

        recorder.Verify(candidate => candidate.ReleaseAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StopAsync_SerializesNewAcquireThroughConfirmedTerminalRelease()
    {
        var disconnectStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var continueDisconnect = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var recorder = new ControlledMicrophoneRecorder(
            beforeStopCompletesAsync: async () =>
            {
                disconnectStarted.TrySetResult();
                await continueDisconnect.Task.ConfigureAwait(false);
            });
        await using var lifecycle = CreateLifecycle(recorder, TimeSpan.FromSeconds(30));
        await using var firstLease = await lifecycle.AcquireAsync(CancellationToken.None);

        var stop = lifecycle.StopAsync(CancellationToken.None);
        await disconnectStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var reacquire = lifecycle.AcquireAsync(CancellationToken.None);

        await Task.Delay(50);
        Assert.False(reacquire.IsCompleted);

        continueDisconnect.TrySetResult();
        await stop.WaitAsync(TimeSpan.FromSeconds(2));
        await using var secondLease = await reacquire.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal(2, recorder.StartCallCount);
        Assert.Equal(1, recorder.ReleaseCallCount);
    }

    [Fact]
    public async Task ReleaseAsync_FromLeaseBeforeStop_DoesNotIdleStopReacquiredGeneration()
    {
        var recorder = new ControlledMicrophoneRecorder();
        var idleTimeout = TimeSpan.FromMilliseconds(100);
        await using var lifecycle = CreateLifecycle(recorder, idleTimeout);
        var firstLease = await lifecycle.AcquireAsync(CancellationToken.None);

        await lifecycle.StopAsync(CancellationToken.None);
        var secondLease = await lifecycle.AcquireAsync(CancellationToken.None);

        await firstLease.DisposeAsync();
        await Task.Delay(idleTimeout + idleTimeout);
        Assert.Equal(1, recorder.ReleaseCallCount);
        Assert.Equal(2, recorder.StartCallCount);

        await secondLease.DisposeAsync();
        await WaitUntilAsync(() => recorder.ReleaseCallCount == 2, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task DisposeAsync_CancelsPendingAccessBeforeStartingTerminalRelease()
    {
        var recorder = new Mock<IMicrophoneRecorder>(MockBehavior.Strict);
        recorder
            .Setup(candidate => candidate.RequestAccessAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken token) => Task.Delay(Timeout.InfiniteTimeSpan, token));
        recorder
            .Setup(candidate => candidate.ReleaseAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var lifecycle = CreateLifecycle(recorder.Object, TimeSpan.FromSeconds(30));

        var access = lifecycle.RequestMicrophoneAccessAsync(CancellationToken.None);
        await Task.Yield();
        await lifecycle.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(2));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => access);
        recorder.Verify(candidate => candidate.ReleaseAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StopAsync_CancelsPendingAccessBeforeWaitingForTheTransitionGate()
    {
        var accessEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var accessCalls = 0;
        var recorder = new Mock<IMicrophoneRecorder>(MockBehavior.Strict);
        recorder
            .Setup(candidate => candidate.RequestAccessAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken token) =>
            {
                if (Interlocked.Increment(ref accessCalls) > 1)
                {
                    return Task.CompletedTask;
                }

                accessEntered.TrySetResult();
                return Task.Delay(Timeout.InfiniteTimeSpan, token);
            });
        recorder
            .Setup(candidate => candidate.ReleaseAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        await using var lifecycle = CreateLifecycle(recorder.Object, TimeSpan.FromSeconds(30));

        var access = lifecycle.RequestMicrophoneAccessAsync(CancellationToken.None);
        await accessEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));

        await lifecycle.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(2));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => access);

        await lifecycle.RequestMicrophoneAccessAsync(CancellationToken.None);
        Assert.Equal(2, Volatile.Read(ref accessCalls));
    }

    [Fact]
    public async Task StopAsync_KeepsAdmissionClosedWhileCancellationCallbacksRun()
    {
        var firstAccessEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task? readmittedAccess = null;
        var accessCalls = 0;
        SpeechCaptureLocalSessionLifecycle? lifecycle = null;
        var recorder = new Mock<IMicrophoneRecorder>(MockBehavior.Strict);
        recorder
            .Setup(candidate => candidate.RequestAccessAsync(It.IsAny<CancellationToken>()))
            .Returns((CancellationToken token) =>
            {
                Interlocked.Increment(ref accessCalls);
                token.Register(() =>
                    readmittedAccess = lifecycle!.RequestMicrophoneAccessAsync(CancellationToken.None));
                firstAccessEntered.TrySetResult();
                return Task.Delay(Timeout.InfiniteTimeSpan, token);
            });
        recorder
            .Setup(candidate => candidate.ReleaseAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        lifecycle = CreateLifecycle(recorder.Object, TimeSpan.FromSeconds(30));
        await using var ownedLifecycle = lifecycle;

        var access = lifecycle.RequestMicrophoneAccessAsync(CancellationToken.None);
        await firstAccessEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));

        await lifecycle.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(2));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => access);
        var rejectedReadmission = Assert.IsAssignableFrom<Task>(readmittedAccess);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => rejectedReadmission);
        Assert.Equal(1, Volatile.Read(ref accessCalls));
    }

    [Fact]
    public async Task DisposeAsync_DuringPendingBrowserAcquisition_TimesOutWhileOwnerCleanupContinues()
    {
        var jsRuntime = new ControlledMicrophoneLifecycleJsRuntime();
        var recorder = new MicrophoneRecorder(jsRuntime);
        var lifecycle = CreateLifecycle(recorder, TimeSpan.FromSeconds(30));
        var access = lifecycle.RequestMicrophoneAccessAsync(CancellationToken.None);
        await jsRuntime.WaitForAcquisitionAsync();

        await Assert.ThrowsAsync<TimeoutException>(() =>
            lifecycle.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(7)));

        jsRuntime.CompleteAcquisition();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => access);
        await WaitUntilAsync(
            () => jsRuntime.ReleaseCallCount == 1,
            TimeSpan.FromSeconds(2));
        await lifecycle.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Equal(1, jsRuntime.ReleaseCallCount);
    }

    [Fact]
    public async Task DisposeAsync_WhenReleaseFailsTwice_RemainsRetryable()
    {
        var recorder = new Mock<IMicrophoneRecorder>(MockBehavior.Strict);
        recorder
            .Setup(candidate => candidate.ReleaseAsync(It.IsAny<CancellationToken>()))
            .Returns(() => recorder.Invocations.Count(invocation => invocation.Method.Name == nameof(IMicrophoneRecorder.ReleaseAsync)) switch
            {
                <= 2 => Task.FromException(new InvalidOperationException("release failed")),
                _ => Task.CompletedTask
            });
        var lifecycle = CreateLifecycle(recorder.Object, TimeSpan.FromSeconds(30));

        await Assert.ThrowsAsync<AggregateException>(() => lifecycle.DisposeAsync().AsTask());
        await lifecycle.DisposeAsync();

        recorder.Verify(candidate => candidate.ReleaseAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task DisposeAsync_RacingQueuedLeaseRelease_DoesNotThrow()
    {
        for (var iteration = 0; iteration < 500; iteration++)
        {
            var disconnectStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var continueDisconnect = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var recorder = new ControlledMicrophoneRecorder(
                beforeStopCompletesAsync: async () =>
                {
                    disconnectStarted.TrySetResult();
                    await continueDisconnect.Task.ConfigureAwait(false);
                });
            var lifecycle = CreateLifecycle(recorder, TimeSpan.FromSeconds(30));
            var lease = await lifecycle.AcquireAsync(CancellationToken.None);

            var lifecycleDispose = lifecycle.DisposeAsync().AsTask();
            await disconnectStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
            var leaseDispose = lease.DisposeAsync().AsTask();
            continueDisconnect.TrySetResult();

            await Task.WhenAll(lifecycleDispose, leaseDispose).WaitAsync(TimeSpan.FromSeconds(2));
        }
    }

    [Fact]
    public async Task DisposeAsync_WithQueuedPause_DoesNotOutliveSessionCoordinator()
    {
        var firstStopEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var continueFirstStop = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondReleaseEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var continueSecondRelease = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var recording = new Mock<IMicrophoneRecording>(MockBehavior.Strict);
        recording.SetupGet(candidate => candidate.Completion).Returns(Task.CompletedTask);
        recording.SetupGet(candidate => candidate.MimeType).Returns("audio/pcm");
        recording.SetupGet(candidate => candidate.Chunks).Returns(EmptyChunks());
        recording.Setup(candidate => candidate.DisposeAsync()).Returns(async () =>
        {
            firstStopEntered.TrySetResult();
            await continueFirstStop.Task.ConfigureAwait(false);
        });
        var releaseCallCount = 0;
        var recorder = new Mock<IMicrophoneRecorder>(MockBehavior.Strict);
        recorder
            .Setup(candidate => candidate.RequestAccessAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        recorder
            .Setup(candidate => candidate.StartAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(recording.Object);
        recorder.Setup(candidate => candidate.ReleaseAsync(It.IsAny<CancellationToken>())).Returns(async () =>
        {
            if (Interlocked.Increment(ref releaseCallCount) == 2)
            {
                secondReleaseEntered.TrySetResult();
                await continueSecondRelease.Task.ConfigureAwait(false);
            }
        });
        var ring = new InMemorySpeechSampleRing(maxLookbackMs: 5_000);
        var captureOptions = new SpeechCaptureClientOptions
        {
            LocalSessionIdleTimeout = TimeSpan.FromSeconds(30),
        };
        using var activeCaptureCoordinator = new SpeechCaptureActiveCaptureCoordinator(
            new NoOpUserSpeechBargeInHandler(),
            NullLogger<SpeechCaptureActiveCaptureCoordinator>.Instance,
            new NoOpBrowserDebugConsole(),
            CreateSegmenterFactory(captureOptions),
            ring,
            new StubMicGateState(isHardOff: false));
        using var sessionCoordinator = new SpeechCaptureSessionCoordinator(
            recorder.Object,
            ring,
            new SpeechAudioMimeTypeResolver(),
            NullLogger<SpeechCaptureSessionCoordinator>.Instance);
        var lifecycle = new SpeechCaptureLocalSessionLifecycle(
            sessionCoordinator,
            new SpeechCaptureChunkDispatcher(activeCaptureCoordinator),
            Options.Create(captureOptions));
        var lease = await lifecycle.AcquireAsync(CancellationToken.None);

        var dispose = lifecycle.DisposeAsync().AsTask();
        await firstStopEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var pause = lifecycle.PauseAsync(CancellationToken.None);
        continueFirstStop.TrySetResult();
        await dispose.WaitAsync(TimeSpan.FromSeconds(2));

        await Task.WhenAny(pause, secondReleaseEntered.Task).WaitAsync(TimeSpan.FromSeconds(2));

        sessionCoordinator.Dispose();
        continueSecondRelease.TrySetResult();

        await pause.WaitAsync(TimeSpan.FromSeconds(2));
        await lease.DisposeAsync();
    }

    private static SpeechCaptureLocalSessionLifecycle CreateLifecycle(
        IMicrophoneRecorder recorder,
        TimeSpan idleTimeout)
    {
        var ring = new InMemorySpeechSampleRing(maxLookbackMs: 5_000);
        var captureOptions = new SpeechCaptureClientOptions
        {

            LocalSessionIdleTimeout = idleTimeout,
        };
        var segmenterFactory = CreateSegmenterFactory(captureOptions);
        var activeCaptureCoordinator = new SpeechCaptureActiveCaptureCoordinator(
            new NoOpUserSpeechBargeInHandler(),
            NullLogger<SpeechCaptureActiveCaptureCoordinator>.Instance,
            new NoOpBrowserDebugConsole(),
            segmenterFactory,
            ring,
            new StubMicGateState(isHardOff: false));
        var dispatcher = new SpeechCaptureChunkDispatcher(activeCaptureCoordinator);
        var sessionCoordinator = new SpeechCaptureSessionCoordinator(
            recorder,
            ring,
            new SpeechAudioMimeTypeResolver(),
            NullLogger<SpeechCaptureSessionCoordinator>.Instance);

        return new SpeechCaptureLocalSessionLifecycle(
            sessionCoordinator,
            dispatcher,
            Options.Create(captureOptions));
    }

    private static SpeechCaptureSegmenterFactory CreateSegmenterFactory(SpeechCaptureClientOptions captureOptions)
    {
        return new SpeechCaptureSegmenterFactory(
            new SpeechCaptureSegmenterOptionsFactory(
                Options.Create(captureOptions),
                Options.Create(new PcmSpeechCaptureSegmenterOptions
                {
                    SpeechStartConfirmation = TimeSpan.FromMilliseconds(40),
                    TailSilence = TimeSpan.FromMilliseconds(80),
                    MaxInitialSilence = TimeSpan.FromSeconds(5),
                })));
    }

    private static async Task WaitUntilAsync(Func<bool> predicate, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);

        while (!cts.IsCancellationRequested)
        {
            if (predicate())
            {
                return;
            }

            await Task.Delay(10, cts.Token).ConfigureAwait(false);
        }

        throw new TimeoutException("Condition was not met before the timeout elapsed.");
    }

    private static async IAsyncEnumerable<byte[]> EmptyChunks()
    {
        await Task.CompletedTask;
        yield break;
    }
}
