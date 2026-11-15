using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Sprechen.Speech.Runtime.Tests.Services;
using Sprechen.Speech.Runtime.Tests.TestDoubles;

namespace Sprechen.Speech.Runtime.Tests.Capture;

public sealed class SpeechCaptureLocalSessionChunkSourceFactoryTests
{
    [Fact]
    public async Task CreateAsync_ReusesWarmSessionBetweenSequentialChunkSources_AndIdlesDownAfterLastRelease()
    {
        var recorder = new ControlledMicrophoneRecorder();
        var (factory, lifecycle) = CreateSubject(recorder, TimeSpan.FromSeconds(1));

        await using var cleanup = new AsyncCleanup(() => lifecycle.DisposeAsync());

        await using (var firstSource = await factory.CreateAsync(CancellationToken.None))
        {
            await firstSource.StopAsync(CancellationToken.None);
        }

        Assert.Equal(1, recorder.StartCallCount);
        Assert.Equal(0, recorder.StopCallCount);

        await using (var secondSource = await factory.CreateAsync(CancellationToken.None))
        {
            await secondSource.StopAsync(CancellationToken.None);
        }

        Assert.Equal(1, recorder.StartCallCount);
        await WaitUntilAsync(() => recorder.StopCallCount == 1, TimeSpan.FromSeconds(5));
        Assert.Equal(1, recorder.RequestAccessCallCount);
    }

    [Fact]
    public async Task CancelActiveCapture_CompletesChunkEnumeration()
    {
        var recorder = new ControlledMicrophoneRecorder();
        var (factory, lifecycle, dispatcher) = CreateSubjectWithDispatcher(recorder, TimeSpan.FromSeconds(1));

        await using var cleanup = new AsyncCleanup(() => lifecycle.DisposeAsync());
        await using var source = (SpeechCaptureLocalSessionChunkSource)await factory.CreateAsync(CancellationToken.None);

        var chunksTask = ReadAllChunksAsync(source.Chunks);

        dispatcher.CancelActiveCaptures(CaptureCancellationKind.Abort, CancellationToken.None);

        var chunks = await chunksTask.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Empty(chunks);
    }

    [Fact]
    public async Task FailActiveCapture_CompletesChunkEnumerationWithFailure()
    {
        var recorder = new ControlledMicrophoneRecorder();
        var (factory, lifecycle, dispatcher) = CreateSubjectWithDispatcher(recorder, TimeSpan.FromSeconds(1));

        await using var cleanup = new AsyncCleanup(() => lifecycle.DisposeAsync());
        await using var source = (SpeechCaptureLocalSessionChunkSource)await factory.CreateAsync(CancellationToken.None);

        var chunksTask = ReadAllChunksAsync(source.Chunks);
        var failure = new InvalidOperationException("microphone session failed");

        dispatcher.FailActiveConsumers(failure);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => chunksTask.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.Same(failure, exception);
    }

    private static (SpeechCaptureLocalSessionChunkSourceFactory Factory, SpeechCaptureLocalSessionLifecycle Lifecycle) CreateSubject(
        ControlledMicrophoneRecorder recorder,
        TimeSpan idleTimeout)
    {
        var (factory, lifecycle, _) = CreateSubjectWithDispatcher(recorder, idleTimeout);
        return (factory, lifecycle);
    }

    private static (
        SpeechCaptureLocalSessionChunkSourceFactory Factory,
        SpeechCaptureLocalSessionLifecycle Lifecycle,
        SpeechCaptureChunkDispatcher Dispatcher) CreateSubjectWithDispatcher(
        ControlledMicrophoneRecorder recorder,
        TimeSpan idleTimeout)
    {
        var ring = new InMemorySpeechSampleRing(maxLookbackMs: 5_000);
        var captureOptions = new SpeechCaptureClientOptions
        {

            LocalSessionIdleTimeout = idleTimeout,
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
        var factory = new SpeechCaptureLocalSessionChunkSourceFactory(lifecycle, dispatcher);

        return (factory, lifecycle, dispatcher);
    }

    private static async Task<List<byte[]>> ReadAllChunksAsync(IAsyncEnumerable<byte[]> chunks)
    {
        var result = new List<byte[]>();

        await foreach (var chunk in chunks)
        {
            result.Add(chunk);
        }

        return result;
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
}
