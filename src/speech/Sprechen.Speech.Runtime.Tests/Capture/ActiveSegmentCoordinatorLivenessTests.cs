using System.Buffers.Binary;
using Microsoft.Extensions.Options;
using Sprechen.Speech.Runtime.Tests.TestDoubles;

namespace Sprechen.Speech.Runtime.Tests.Capture;

public sealed class ActiveSegmentCoordinatorLivenessTests
{
    private const int SampleRateHz = InMemorySpeechSampleRing.SampleRateHz;
    private const int BytesPerSample = InMemorySpeechSampleRing.BytesPerSample;

    [Fact]
    public async Task CaptureNextSegmentAsync_WhenNoChunkEverArrives_FailsWithStallAfterLivenessWindow()
    {
        using var coordinator = CreateCoordinator(TimeSpan.FromMilliseconds(200), out _);
        coordinator.ResetToLiveEdge();

        // Ohne äußere Zeitgrenze muss ausbleibendes Audio über die eigene Überwachung zum Fehler führen.
        var capture = coordinator.CaptureNextSegmentAsync(
            SpeechAudioMimeType.Pcm,
            silenceDebounceMs: 20,
            SpeechCaptureStartContext.CleanListening,
            SpeechCaptureModeBehavior.For(SpeechCaptureMode.ActivationListening),
            CancellationToken.None);
        await Assert.ThrowsAsync<SpeechCaptureStreamStalledException>(
            () => capture.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task CaptureNextSegmentAsync_WhenChunksStopMidUtterance_FailsWithStallAfterLivenessWindow()
    {
        using var coordinator = CreateCoordinator(TimeSpan.FromMilliseconds(200), out var ring);
        coordinator.ResetToLiveEdge();

        var capture = coordinator.CaptureNextSegmentAsync(
            SpeechAudioMimeType.Pcm,
            silenceDebounceMs: 20,
            SpeechCaptureStartContext.CleanListening,
            SpeechCaptureModeBehavior.For(SpeechCaptureMode.ActivationListening),
            CancellationToken.None);

        // Nach Sprachbeginn kommen keine weiteren Blöcke und kein Endsignal; dies simuliert einen Stillstand während der Äußerung.
        for (var i = 0; i < 8; i++)
        {
            var chunk = CreatePcmChunk(20, sampleValue: 1000);
            ring.WriteSamples(chunk);
            await coordinator.ProcessChunkAsync(chunk, CancellationToken.None);
        }

        await Assert.ThrowsAsync<SpeechCaptureStreamStalledException>(
            () => capture.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task ProcessChunkAsync_WhenSpeechOnsetTraceNeverReturns_StillCompletesHealthyCapture()
    {
        var debugConsole = new MessageBlockingBrowserDebugConsole("speech onset confirmed");
        using var coordinator = CreateCoordinator(
            TimeSpan.FromSeconds(1),
            out var ring,
            debugConsole);
        coordinator.ResetToLiveEdge();

        var capture = coordinator.CaptureNextSegmentAsync(
            SpeechAudioMimeType.Pcm,
            silenceDebounceMs: 20,
            SpeechCaptureStartContext.CleanListening,
            SpeechCaptureModeBehavior.For(SpeechCaptureMode.ActivationListening),
            CancellationToken.None);
        await debugConsole.CaptureArmed.Task.WaitAsync(TimeSpan.FromSeconds(2));

        for (var i = 0; i < 20; i++)
        {
            var chunk = CreatePcmChunk(20, sampleValue: 1000);
            ring.WriteSamples(chunk);
            await coordinator.ProcessChunkAsync(chunk, CancellationToken.None);
        }

        await debugConsole.BlockingWriteStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        for (var i = 0; i < 80; i++)
        {
            var silence = CreatePcmChunk(20, sampleValue: 0);
            ring.WriteSamples(silence);
            await coordinator.ProcessChunkAsync(silence, CancellationToken.None);
        }

        try
        {
            var utterance = await capture.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.NotEmpty(utterance.Chunks);
        }
        finally
        {
            debugConsole.ContinueBlockingWrite.TrySetResult();
        }
    }

    [Fact]
    public async Task AwaitAsync_WhenChunkArrivesDuringStallTrace_DoesNotFailHealthyCapture()
    {
        var debugConsole = new FirstWriteBlockingBrowserDebugConsole();
        var guard = new CaptureChunkLivenessGuard(
            TimeSpan.FromSeconds(1),
            debugConsole,
            "capture stalled");
        var capture = new TaskCompletionSource<CapturedSpeechAudio>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var failed = false;
        var guardedCapture = guard.AwaitAsync(
            capture.Task,
            _ => failed = true,
            CancellationToken.None);

        try
        {
            await debugConsole.FirstWriteStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            guard.ObserveChunk();
            debugConsole.ContinueFirstWrite.TrySetResult();
            await Task.Delay(400);
            Assert.False(failed);

            var expected = new CapturedSpeechAudio(SpeechAudioMimeType.Pcm, [new byte[] { 1 }]);
            capture.TrySetResult(expected);
            Assert.Same(expected, await guardedCapture.WaitAsync(TimeSpan.FromSeconds(2)));
        }
        finally
        {
            debugConsole.ContinueFirstWrite.TrySetResult();
            capture.TrySetCanceled();
            try
            {
                await guardedCapture.WaitAsync(TimeSpan.FromSeconds(2));
            }
            catch (OperationCanceledException)
            {
            }
        }
    }

    [Fact]
    public async Task AwaitAsync_WhenStallTraceNeverCompletes_StillFailsCapture()
    {
        var debugConsole = new FirstWriteBlockingBrowserDebugConsole();
        var guard = new CaptureChunkLivenessGuard(
            TimeSpan.FromMilliseconds(50),
            debugConsole,
            "capture stalled");
        var capture = new TaskCompletionSource<CapturedSpeechAudio>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var guardedCapture = guard.AwaitAsync(
            capture.Task,
            exception => capture.TrySetException(exception),
            CancellationToken.None);

        try
        {
            await debugConsole.FirstWriteStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
            await Assert.ThrowsAsync<SpeechCaptureStreamStalledException>(
                () => guardedCapture.WaitAsync(TimeSpan.FromSeconds(2)));
        }
        finally
        {
            debugConsole.ContinueFirstWrite.TrySetResult();
            capture.TrySetCanceled();
            try
            {
                await guardedCapture.WaitAsync(TimeSpan.FromSeconds(2));
            }
            catch (OperationCanceledException)
            {
            }
            catch (SpeechCaptureStreamStalledException)
            {
            }
        }
    }

    private static ActiveSegmentCoordinator CreateCoordinator(
        TimeSpan livenessTimeout,
        out InMemorySpeechSampleRing ring,
        IBrowserDebugConsole? browserDebugConsole = null)
    {
        var optionsFactory = new SpeechCaptureSegmenterOptionsFactory(
            Options.Create(new SpeechCaptureClientOptions()),
            Options.Create(new PcmSpeechCaptureSegmenterOptions
            {
                SpeechStartConfirmation = TimeSpan.FromMilliseconds(40),
                TailSilence = TimeSpan.FromMilliseconds(20),
                MaxInitialSilence = TimeSpan.FromSeconds(30),
            }));
        var factory = new SpeechCaptureSegmenterFactory(optionsFactory);
        ring = new InMemorySpeechSampleRing(maxLookbackMs: 5_000);
        return new ActiveSegmentCoordinator(
            factory,
            ring,
            browserDebugConsole ?? new NoOpBrowserDebugConsole(),
            Options.Create(new SpeechCaptureClientOptions { ChunkArrivalLivenessTimeout = livenessTimeout }));
    }

    private static byte[] CreatePcmChunk(int durationMs, short sampleValue)
    {
        var bytes = new byte[durationMs * SampleRateHz * BytesPerSample / 1000];
        for (var i = 0; i < bytes.Length; i += BytesPerSample)
        {
            BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(i, BytesPerSample), sampleValue);
        }

        return bytes;
    }
}
