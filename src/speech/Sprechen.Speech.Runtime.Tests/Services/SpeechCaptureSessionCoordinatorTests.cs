using Bunit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sprechen.Speech.App.ClientInfrastructure.Services;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class SpeechCaptureSessionCoordinatorTests : TestContext
{
    [Fact]
    public async Task Dispose_DuringMicrophoneRelease_DoesNotPoisonTheInFlightCleanup()
    {
        var releaseEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var continueRelease = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var recorder = new Mock<IMicrophoneRecorder>(MockBehavior.Strict);
        recorder
            .Setup(candidate => candidate.ReleaseAsync(It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                releaseEntered.TrySetResult();
                await continueRelease.Task.ConfigureAwait(false);
            });
        var coordinator = new SpeechCaptureSessionCoordinator(
            recorder.Object,
            new InMemorySpeechSampleRing(maxLookbackMs: 5_000),
            new SpeechAudioMimeTypeResolver(),
            NullLogger<SpeechCaptureSessionCoordinator>.Instance);

        var release = coordinator.ReleaseMicrophoneAsync(CancellationToken.None);
        await releaseEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        coordinator.Dispose();
        continueRelease.TrySetResult();

        await release.WaitAsync(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task SessionCompletion_IsPublishedBeforeTheTerminalCallback()
    {
        var recorder = new ControlledMicrophoneRecorder();
        var coordinator = new SpeechCaptureSessionCoordinator(
            recorder,
            new InMemorySpeechSampleRing(maxLookbackMs: 5_000),
            new SpeechAudioMimeTypeResolver(),
            NullLogger<SpeechCaptureSessionCoordinator>.Instance);
        SpeechCaptureSessionHandle handle = default;
        var callbackObservedCompletion = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        try
        {
            handle = await coordinator.EnsureSessionStartedAsync(
                static (_, _) => Task.CompletedTask,
                _ => callbackObservedCompletion.TrySetResult(handle.Completion.IsCompleted),
                CancellationToken.None);

            recorder.CompleteRecording();

            Assert.True(await callbackObservedCompletion.Task.WaitAsync(TimeSpan.FromSeconds(2)));
            Assert.True(handle.Completion.IsCompleted);
        }
        finally
        {
            await coordinator.StopAsync(static _ => { }, CancellationToken.None);
            coordinator.Dispose();
        }
    }

    [Fact]
    public async Task ReplacementSession_WaitsUntilThePreviousTerminalCallbackHasFinished()
    {
        var recorder = new ControlledMicrophoneRecorder();
        var coordinator = new SpeechCaptureSessionCoordinator(
            recorder,
            new InMemorySpeechSampleRing(maxLookbackMs: 5_000),
            new SpeechAudioMimeTypeResolver(),
            NullLogger<SpeechCaptureSessionCoordinator>.Instance);
        var callbackEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseCallback = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        try
        {
            var first = await coordinator.EnsureSessionStartedAsync(
                static (_, _) => Task.CompletedTask,
                _ =>
                {
                    callbackEntered.TrySetResult();
                    releaseCallback.Task.GetAwaiter().GetResult();
                },
                CancellationToken.None);

            recorder.CompleteRecording();
            await callbackEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.True(first.Completion.IsCompleted);

            var replacement = coordinator.EnsureSessionStartedAsync(
                static (_, _) => Task.CompletedTask,
                static _ => { },
                CancellationToken.None);
            await Task.Delay(50);

            Assert.False(replacement.IsCompleted);
            Assert.Equal(1, recorder.StartCallCount);

            releaseCallback.TrySetResult();
            var second = await replacement.WaitAsync(TimeSpan.FromSeconds(2));

            Assert.False(second.Completion.IsCompleted);
            Assert.Equal(2, recorder.StartCallCount);
        }
        finally
        {
            releaseCallback.TrySetResult();
            await coordinator.StopAsync(static _ => { }, CancellationToken.None);
            coordinator.Dispose();
        }
    }

    [Fact]
    public async Task RecordingCompletion_IsPublishedWhileAnEarlierChunkCallbackStillRuns()
    {
        var recorder = new ControlledMicrophoneRecorder();
        var coordinator = new SpeechCaptureSessionCoordinator(
            recorder,
            new InMemorySpeechSampleRing(maxLookbackMs: 5_000),
            new SpeechAudioMimeTypeResolver(),
            NullLogger<SpeechCaptureSessionCoordinator>.Instance);
        var callbackEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseCallback = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        try
        {
            var first = await coordinator.EnsureSessionStartedAsync(
                async (_, _) =>
                {
                    callbackEntered.TrySetResult();
                    await releaseCallback.Task.ConfigureAwait(false);
                },
                static _ => { },
                CancellationToken.None);

            recorder.PushChunk([1, 2]);
            await callbackEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));
            recorder.CompleteRecording();

            Assert.True(first.RecordingCompletion.IsCompleted);
            Assert.False(first.Completion.IsCompleted);

            var replacement = coordinator.EnsureSessionStartedAsync(
                static (_, _) => Task.CompletedTask,
                static _ => { },
                CancellationToken.None);
            await Task.Delay(50);
            Assert.False(replacement.IsCompleted);
            Assert.Equal(1, recorder.StartCallCount);

            releaseCallback.TrySetResult();
            var second = await replacement.WaitAsync(TimeSpan.FromSeconds(2));

            Assert.False(second.RecordingCompletion.IsCompleted);
            Assert.Equal(2, recorder.StartCallCount);
        }
        finally
        {
            releaseCallback.TrySetResult();
            await coordinator.StopAsync(static _ => { }, CancellationToken.None);
            coordinator.Dispose();
        }
    }

    [Fact]
    public async Task ReplacementSession_WhenPreviousChunkCallbackIsBlocked_HonorsCallerCancellation()
    {
        var recorder = new ControlledMicrophoneRecorder();
        var coordinator = new SpeechCaptureSessionCoordinator(
            recorder,
            new InMemorySpeechSampleRing(maxLookbackMs: 5_000),
            new SpeechAudioMimeTypeResolver(),
            NullLogger<SpeechCaptureSessionCoordinator>.Instance);
        var callbackEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseCallback = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        try
        {
            var first = await coordinator.EnsureSessionStartedAsync(
                async (_, _) =>
                {
                    callbackEntered.TrySetResult();
                    await releaseCallback.Task.ConfigureAwait(false);
                },
                static _ => { },
                CancellationToken.None);

            recorder.PushChunk([1, 2]);
            await callbackEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));
            recorder.CompleteRecording();
            Assert.True(first.RecordingCompletion.IsCompleted);

            using var replacementCts = new CancellationTokenSource();
            var replacement = coordinator.EnsureSessionStartedAsync(
                static (_, _) => Task.CompletedTask,
                static _ => { },
                replacementCts.Token);
            replacementCts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                replacement.WaitAsync(TimeSpan.FromSeconds(2)));
            Assert.Equal(1, recorder.StartCallCount);
            Assert.False(releaseCallback.Task.IsCompleted);

            var secondReplacement = coordinator.EnsureSessionStartedAsync(
                static (_, _) => Task.CompletedTask,
                static _ => { },
                CancellationToken.None);
            await Task.Delay(50);

            Assert.False(secondReplacement.IsCompleted);
            Assert.Equal(1, recorder.StartCallCount);

            releaseCallback.TrySetResult();
            var second = await secondReplacement.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.False(second.RecordingCompletion.IsCompleted);
            Assert.Equal(2, recorder.StartCallCount);
        }
        finally
        {
            releaseCallback.TrySetResult();
            await coordinator.StopAsync(static _ => { }, CancellationToken.None);
            coordinator.Dispose();
        }
    }

    [Fact]
    public async Task StopAsync_AfterCancelledReplacement_WaitsForRetainedPumpShutdown()
    {
        var recorder = new ControlledMicrophoneRecorder();
        var coordinator = new SpeechCaptureSessionCoordinator(
            recorder,
            new InMemorySpeechSampleRing(maxLookbackMs: 5_000),
            new SpeechAudioMimeTypeResolver(),
            NullLogger<SpeechCaptureSessionCoordinator>.Instance);
        var callbackEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseCallback = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Task? stop = null;

        try
        {
            await coordinator.EnsureSessionStartedAsync(
                async (_, _) =>
                {
                    callbackEntered.TrySetResult();
                    await releaseCallback.Task.ConfigureAwait(false);
                },
                static _ => { },
                CancellationToken.None);

            recorder.PushChunk([1, 2]);
            await callbackEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));
            recorder.CompleteRecording();

            using var replacementCancellation = new CancellationTokenSource();
            var replacement = coordinator.EnsureSessionStartedAsync(
                static (_, _) => Task.CompletedTask,
                static _ => { },
                replacementCancellation.Token);
            await Task.Delay(50);
            replacementCancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                replacement.WaitAsync(TimeSpan.FromSeconds(2)));

            stop = coordinator.StopAsync(static _ => { }, CancellationToken.None);
            await Task.Delay(100);

            Assert.False(stop.IsCompleted);
        }
        finally
        {
            releaseCallback.TrySetResult();
            if (stop is not null)
            {
                await stop.WaitAsync(TimeSpan.FromSeconds(2));
            }

            coordinator.Dispose();
        }
    }

    [Fact]
    public async Task ReplacementSession_DuringStop_WaitsUntilThePreviousCallbackHasFinished()
    {
        var recorder = new ControlledMicrophoneRecorder();
        var coordinator = new SpeechCaptureSessionCoordinator(
            recorder,
            new InMemorySpeechSampleRing(maxLookbackMs: 5_000),
            new SpeechAudioMimeTypeResolver(),
            NullLogger<SpeechCaptureSessionCoordinator>.Instance);
        var callbackEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseCallback = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        try
        {
            await coordinator.EnsureSessionStartedAsync(
                async (_, _) =>
                {
                    callbackEntered.TrySetResult();
                    await releaseCallback.Task.ConfigureAwait(false);
                },
                static _ => { },
                CancellationToken.None);

            recorder.PushChunk([1, 2]);
            await callbackEntered.Task.WaitAsync(TimeSpan.FromSeconds(2));

            var stop = coordinator.StopAsync(static _ => { }, CancellationToken.None);
            await Task.Delay(50);
            Assert.False(stop.IsCompleted);

            var replacement = coordinator.EnsureSessionStartedAsync(
                static (_, _) => Task.CompletedTask,
                static _ => { },
                CancellationToken.None);
            await Task.Delay(50);

            Assert.False(replacement.IsCompleted);
            Assert.Equal(1, recorder.StartCallCount);

            releaseCallback.TrySetResult();
            await stop.WaitAsync(TimeSpan.FromSeconds(2));
            var second = await replacement.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.False(second.RecordingCompletion.IsCompleted);
            Assert.Equal(2, recorder.StartCallCount);
        }
        finally
        {
            releaseCallback.TrySetResult();
            await coordinator.StopAsync(static _ => { }, CancellationToken.None);
            coordinator.Dispose();
        }
    }

    [Fact]
    public async Task EnsureSessionStartedAsync_WhenPreviousSessionFaulted_RestartsMicrophoneSession()
    {
        JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true).SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        JSInterop.Setup<string>("micRecorder.connect", _ => true).SetResult("audio/pcm;rate=16000");
        JSInterop.SetupVoid("micRecorder.disconnect", _ => true).SetVoidResult();

        await using var firstRecording = await MicrophoneRecordingTestFactory.StartAsync(JSInterop.JSRuntime, timesliceMilliseconds: 20, CancellationToken.None);
        await using var secondRecording = await MicrophoneRecordingTestFactory.StartAsync(JSInterop.JSRuntime, timesliceMilliseconds: 20, CancellationToken.None);

        var microphoneRecorder = new Mock<IMicrophoneRecorder>(MockBehavior.Strict);
        microphoneRecorder
            .Setup(recorder => recorder.RequestAccessAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var recordings = new Queue<IMicrophoneRecording>([firstRecording, secondRecording]);
        var startCount = 0;
        microphoneRecorder
            .Setup(recorder => recorder.StartAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                startCount++;
                return recordings.Dequeue();
            });

        var failures = new List<Exception>();
        var failureSignal = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        var coordinator = new SpeechCaptureSessionCoordinator(
            microphoneRecorder.Object,
            new InMemorySpeechSampleRing(maxLookbackMs: 5_000),
            new SpeechAudioMimeTypeResolver(),
            NullLogger<SpeechCaptureSessionCoordinator>.Instance);

        try
        {
            await coordinator.RequestMicrophoneAccessAsync(CancellationToken.None);
            var firstSession = await coordinator.EnsureSessionStartedAsync(
                static (_, _) => Task.CompletedTask,
                exception =>
                {
                    failures.Add(exception);
                    failureSignal.TrySetResult(exception);
                },
                CancellationToken.None);

            firstRecording.OnError("boom");
            var failure = await failureSignal.Task.WaitAsync(TimeSpan.FromSeconds(5));

            SpeechCaptureSessionHandle secondSession = default;
            var restartDeadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(5);
            while (startCount < 2 && DateTimeOffset.UtcNow < restartDeadline)
            {
                secondSession = await coordinator.EnsureSessionStartedAsync(
                    static (_, _) => Task.CompletedTask,
                    exception =>
                    {
                        failures.Add(exception);
                        failureSignal.TrySetResult(exception);
                    },
                    CancellationToken.None);

                if (startCount < 2)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(10));
                }
            }

            Assert.Equal(SpeechAudioMimeType.Pcm, firstSession.MimeType);
            Assert.Equal(SpeechAudioMimeType.Pcm, secondSession.MimeType);
            Assert.Single(failures);
            Assert.Equal("boom", failure.Message);
            microphoneRecorder.Verify(recorder => recorder.StartAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        }
        finally
        {
            await coordinator.StopAsync(static _ => { }, CancellationToken.None);
            coordinator.Dispose();
        }
    }
}
