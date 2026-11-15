using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Sprechen.Speech.App.BrowserInfrastructure.Services;
using Sprechen.Speech.Runtime.Tests.Services;
using Sprechen.Speech.Runtime.Tests.TestDoubles;

namespace Sprechen.Speech.Runtime.Tests.Capture;

/// <summary>
/// Misst Allokationen vom Mikrofoncallback über Kanäle, Sitzungspumpe, Ring und
/// Verteiler bis zu einer angemeldeten lokalen Audioquelle. Die 664-Byte-Arrays als
/// Interop-Ersatz entstehen vor dem Messfenster. Das Budget erlaubt eine Kopie des
/// 640-Byte-Audioblocks sowie Wachstum der beiden unbegrenzten Kanäle.
/// </summary>
public sealed class SpeechCaptureChunkPathAllocationTests
{
    private const int SampleRateHz = InMemorySpeechSampleRing.SampleRateHz;
    private const int BytesPerSample = InMemorySpeechSampleRing.BytesPerSample;
    private const int ChunkMs = 20;
    private const int LiveFrameBytes = SampleRateHz * BytesPerSample * ChunkMs / 1000;
    private const int MarshalledChunkBytes = 664;
    private const int MeasuredChunks = 2_000;
    private const int WarmupChunks = 64;

    // 32 Bytes je Block erlauben Kanalwachstum und anteilige Pumpenkosten;
    // eine zweite 640-Byte-Kopie überschreitet diesen Spielraum deutlich.
    private const int ChannelGrowthAllowanceBytesPerChunk = 32;

    [Fact]
    public async Task OnChunkThroughPumpAndDispatcher_AllocatesAtMostOneLiveFramePlusChannelGrowth()
    {
        var chunks = new byte[MeasuredChunks + WarmupChunks][];
        for (var i = 0; i < chunks.Length; i++)
        {
            chunks[i] = new byte[MarshalledChunkBytes];
        }

        var recorder = new ControlledMicrophoneRecorder();
        var (factory, lifecycle, dispatcher) = CreateDispatcherGraph(recorder);
        await using var cleanup = new AsyncCleanup(() => lifecycle.DisposeAsync());
        await using var source = await factory.CreateAsync(CancellationToken.None);
        using var drainCts = new CancellationTokenSource();
        var drain = DrainAsync(source.Chunks, drainCts.Token);

        var ring = new InMemorySpeechSampleRing(maxLookbackMs: 5_000);
        await RunPathAsync(ring, dispatcher, chunks, offset: 0, count: WarmupChunks);

        var before = GC.GetAllocatedBytesForCurrentThread();
        await RunPathAsync(ring, dispatcher, chunks, offset: WarmupChunks, count: MeasuredChunks);
        var allocatedPerChunk = (GC.GetAllocatedBytesForCurrentThread() - before) / MeasuredChunks;

        drainCts.Cancel();
        try
        {
            await drain;
        }
        catch (OperationCanceledException)
        {
        }

        var bound = LiveFrameBytes + ChannelGrowthAllowanceBytesPerChunk;
        Assert.True(
            allocatedPerChunk < bound,
            $"chunk path allocated ~{allocatedPerChunk} B/chunk; expected < {bound} B (640 B live frame + {ChannelGrowthAllowanceBytesPerChunk} B channel growth).");
    }

    private static async Task RunPathAsync(
        InMemorySpeechSampleRing ring,
        SpeechCaptureChunkDispatcher dispatcher,
        byte[][] chunks,
        int offset,
        int count)
    {
        var recording = new MicrophoneRecording(static (_, _) => Task.CompletedTask);
        var pump = new SpeechCaptureSessionPump(ring, NullLogger.Instance);
        for (var i = 0; i < count; i++)
        {
            recording.OnChunk(chunks[offset + i]);
        }

        recording.OnStopped();
        await pump.RunAsync(
            recording,
            dispatcher.ProcessChunkAsync,
            static _ => { },
            CancellationToken.None);
    }

    private static async Task DrainAsync(IAsyncEnumerable<byte[]> chunks, CancellationToken cancellationToken)
    {
        await foreach (var _ in chunks.WithCancellation(cancellationToken))
        {
        }
    }

    private static (
        SpeechCaptureLocalSessionChunkSourceFactory Factory,
        SpeechCaptureLocalSessionLifecycle Lifecycle,
        SpeechCaptureChunkDispatcher Dispatcher) CreateDispatcherGraph(
        ControlledMicrophoneRecorder recorder)
    {
        var ring = new InMemorySpeechSampleRing(maxLookbackMs: 5_000);
        var captureOptions = new SpeechCaptureClientOptions
        {
            LocalSessionIdleTimeout = TimeSpan.FromMinutes(10),
        };
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
        var lifecycle = new SpeechCaptureLocalSessionLifecycle(
            sessionCoordinator,
            dispatcher,
            Options.Create(captureOptions));
        return (new SpeechCaptureLocalSessionChunkSourceFactory(lifecycle, dispatcher), lifecycle, dispatcher);
    }
}
