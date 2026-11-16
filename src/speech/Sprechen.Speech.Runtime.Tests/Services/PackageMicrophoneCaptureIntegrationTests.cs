using System.Collections;
using MicroSharp;
using MicroSharp.Browser;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using Xunit.Abstractions;

namespace Sprechen.Speech.Runtime.Tests.Services;

/// <summary>
/// Führt synthetisches PCM durch das Paket in die Aufnahme, den Ring und den Segmentierer.
/// Die Laufzeit spricht nur die Paketschnittstelle und weist micRecorder zurück.
/// </summary>
public sealed class PackageMicrophoneCaptureIntegrationTests
{
    private readonly ITestOutputHelper _output;

    public PackageMicrophoneCaptureIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task SyntheticPcmReachesTheRingAndSegmenter_StopRestartsAndRelease()
    {
        var js = new PackageMicrophoneJsRuntime();
        MicrophoneJsTestBridge.Bypass.Value = true;
        try
        {
            var recorder = new MicrophoneRecorder(js);
            await using var recording = await recorder.StartAsync(CancellationToken.None);
            var typed = Assert.IsType<MicrophoneRecording>(recording);
            var session = js.StartedSessions.Single();

            Assert.Equal("audio/pcm;rate=16000", typed.MimeType);
            Assert.DoesNotContain(js.Calls, call => call.StartsWith("micRecorder", StringComparison.Ordinal));
            Assert.Contains("acquire", js.Calls);
            Assert.Contains("start", js.Calls);
            Assert.Equal(true, js.LastAcquirePreference("echoCancellation"));
            Assert.Equal(true, js.LastAcquirePreference("noiseSuppression"));
            Assert.Equal(true, js.LastAcquirePreference("autoGainControl"));
            Assert.Equal(true, js.LastAcquirePreference("keepAudioContextAlive"));

            for (var i = 0; i < 60; i++)
            {
                session.OnPcmChunk(LoudChunk());
            }

            Assert.Equal(10, typed.CaptureDroppedChunkCount);

            var ring = new InMemorySpeechSampleRing(5_000);
            var segmenter = new PcmSpeechCaptureSegmenter(
                SpeechAudioMimeType.Pcm,
                silenceDebounceMs: 3_000,
                new PcmSpeechCaptureSegmenterOptions());
            var speechStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Exception? sessionFailure = null;
            var pump = new SpeechCaptureSessionPump(ring, NullLogger.Instance);
            var pumpTask = pump.RunAsync(
                recording,
                (chunk, _) =>
                {
                    if (chunk.Length != 640)
                    {
                        throw new InvalidOperationException($"Unexpected chunk length {chunk.Length}.");
                    }

                    if (segmenter.ProcessChunk(chunk).SpeechStarted)
                    {
                        speechStarted.TrySetResult();
                    }

                    return Task.CompletedTask;
                },
                failure => sessionFailure = failure,
                CancellationToken.None);

            var confirmed = await Task.WhenAny(speechStarted.Task, Task.Delay(TimeSpan.FromSeconds(5)));
            Assert.Same(speechStarted.Task, confirmed);
            await recorder.StopAsync(CancellationToken.None);
            await pumpTask.WaitAsync(TimeSpan.FromSeconds(5));

            Assert.IsType<SpeechCaptureSessionEndedException>(sessionFailure);
            Assert.True(ring.WriteByteIndex > 0);
            var read = new byte[640];
            var count = ring.Read(ring.OpenCursor(SampleCursorOrigin.Earliest), read);
            Assert.Equal(640, count);
            Assert.Contains(read, value => value != 0);
            Assert.Equal(1, js.Calls.Count(call => call == "disconnect"));
            Assert.DoesNotContain("release", js.Calls);

            await using var restarted = await recorder.StartAsync(CancellationToken.None);
            Assert.Equal(typed.MimeType, restarted.MimeType);
            Assert.Equal(2, js.Calls.Count(call => call == "start"));
            Assert.Equal(1, js.Calls.Count(call => call == "acquire"));
            Assert.Contains("isLive", js.Calls);
            Assert.DoesNotContain("release", js.Calls);

            var disconnectsBeforeOlderDispose = js.Calls.Count(call => call == "disconnect");
            await typed.DisposeAsync();
            Assert.Equal(disconnectsBeforeOlderDispose, js.Calls.Count(call => call == "disconnect"));
            var replacement = js.StartedSessions[^1];
            Assert.Equal(MicrophoneSessionState.Running, replacement.State);
            var payload = LoudChunk();
            replacement.OnPcmChunk(payload);
            await using var reader = replacement.ReadChunksAsync().GetAsyncEnumerator();
            Assert.True(await reader.MoveNextAsync());
            Assert.Equal(payload, reader.Current.Payload);

            await recorder.ReleaseAsync(CancellationToken.None);
            Assert.Contains("release", js.Calls);
            Assert.False(await js.IsLiveAsync());
        }
        finally
        {
            MicrophoneJsTestBridge.Bypass.Value = false;
        }
    }

    [Fact]
    public async Task CancellingThePumpBeforeDequeueLeavesThePackageSessionRunning()
    {
        var js = new PackageMicrophoneJsRuntime();
        MicrophoneJsTestBridge.Bypass.Value = true;
        try
        {
            var recorder = new MicrophoneRecorder(js);
            await using var recording = await recorder.StartAsync(CancellationToken.None);
            var session = js.StartedSessions.Single();
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            var pump = new SpeechCaptureSessionPump(new InMemorySpeechSampleRing(5_000), NullLogger.Instance);
            Exception? sessionFailure = null;
            await pump.RunAsync(
                recording,
                (_, _) => Task.CompletedTask,
                failure => sessionFailure = failure,
                cancellation.Token);

            Assert.Null(sessionFailure);
            Assert.Equal(MicrophoneSessionState.Running, session.State);
            Assert.DoesNotContain("disconnect", js.Calls);
            Assert.DoesNotContain("release", js.Calls);
        }
        finally
        {
            MicrophoneJsTestBridge.Bypass.Value = false;
        }
    }

    [Fact]
    public async Task WrongLengthChunkFaultsThePackageSession()
    {
        var js = new PackageMicrophoneJsRuntime();
        MicrophoneJsTestBridge.Bypass.Value = true;
        try
        {
            var recorder = new MicrophoneRecorder(js);
            await using var recording = await recorder.StartAsync(CancellationToken.None);
            js.StartedSessions.Single().OnPcmChunk(new byte[4]);

            var failure = await Assert.ThrowsAsync<MicrophoneException>(async () =>
            {
                await foreach (var _ in recording.Chunks)
                {
                }
            });

            Assert.Equal(MicrophoneErrorCode.InvalidChunk, failure.Code);
            Assert.Contains("Expected 640 bytes", failure.Message, StringComparison.Ordinal);
        }
        finally
        {
            MicrophoneJsTestBridge.Bypass.Value = false;
        }
    }

    [Fact]
    public async Task NotAllowedErrorMapsToPermissionDenied()
    {
        var js = new PackageMicrophoneJsRuntime { AcquireErrorName = "NotAllowedError" };
        MicrophoneJsTestBridge.Bypass.Value = true;
        try
        {
            var recorder = new MicrophoneRecorder(js);
            var failure = await Assert.ThrowsAsync<MicrophoneAcquisitionException>(
                () => recorder.RequestAccessAsync(CancellationToken.None));

            Assert.Equal(MicrophoneAcquisitionFailureCode.PermissionDenied, failure.Code);
            Assert.DoesNotContain(js.Calls, call => call.StartsWith("micRecorder", StringComparison.Ordinal));
            Assert.DoesNotContain("start", js.Calls);
        }
        finally
        {
            MicrophoneJsTestBridge.Bypass.Value = false;
        }
    }

    [Fact]
    public async Task ReportedRateMismatchIsRejected()
    {
        var js = new PackageMicrophoneJsRuntime { ReportedSampleRateHz = 48_000 };
        MicrophoneJsTestBridge.Bypass.Value = true;
        try
        {
            var recorder = new MicrophoneRecorder(js);
            var failure = await Assert.ThrowsAsync<MicrophoneAcquisitionException>(
                () => recorder.StartAsync(CancellationToken.None));
            var cause = Assert.IsType<InvalidOperationException>(failure.InnerException);
            Assert.Contains("rate=48000", cause.Message, StringComparison.Ordinal);
            Assert.Contains("rate=16000", cause.Message, StringComparison.Ordinal);
        }
        finally
        {
            MicrophoneJsTestBridge.Bypass.Value = false;
        }
    }

    [Fact]
    public async Task OnChunkDoesNotSupplyAudioToAnAttachedRecording()
    {
        var js = new PackageMicrophoneJsRuntime();
        MicrophoneJsTestBridge.Bypass.Value = true;
        try
        {
            var recorder = new MicrophoneRecorder(js);
            await using var recording = await recorder.StartAsync(CancellationToken.None);
            var typed = Assert.IsType<MicrophoneRecording>(recording);
            var marker = new byte[] { 9, 8, 7 };
            typed.OnChunk(marker);
            typed.OnError("attached callback must not fault the package reader");
            Assert.Equal(MicrophoneSessionState.Running, js.StartedSessions.Single().State);

            using var idle = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
            var sawMarker = false;
            try
            {
                await foreach (var chunk in recording.Chunks.WithCancellation(idle.Token))
                {
                    if (chunk.Length == marker.Length && chunk.AsSpan().SequenceEqual(marker))
                    {
                        sawMarker = true;
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }

            Assert.False(sawMarker);
            var payload = LoudChunk();
            js.StartedSessions.Single().OnPcmChunk(payload);
            await using var reader = recording.Chunks.GetAsyncEnumerator();
            Assert.True(await reader.MoveNextAsync());
            Assert.Equal(payload, reader.Current);
        }
        finally
        {
            MicrophoneJsTestBridge.Bypass.Value = false;
        }
    }

    [Fact]
    public async Task ProductionRecordingWithoutASessionHasNoInjectionQueue()
    {
        await using var recording = new MicrophoneRecording(static (candidate, _) =>
        {
            candidate.OnStopped();
            return Task.CompletedTask;
        });
        recording.OnChunk(new byte[] { 1, 2, 3 });
        var failure = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in recording.Chunks)
            {
            }
        });

        Assert.Contains("no capture session", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EarlyBreakAfterOnePackageChunkReleasesTheReaderWithoutStopping()
    {
        var js = new PackageMicrophoneJsRuntime();
        MicrophoneJsTestBridge.Bypass.Value = true;
        try
        {
            var recorder = new MicrophoneRecorder(js);
            await using var recording = await recorder.StartAsync(CancellationToken.None);
            var session = js.StartedSessions.Single();
            var first = LoudChunk();
            var second = LoudChunk();
            second[0] = 0x11;
            session.OnPcmChunk(first);
            session.OnPcmChunk(second);

            byte[]? observed = null;
            await foreach (var chunk in recording.Chunks)
            {
                observed = chunk;
                break;
            }

            Assert.Equal(first, observed);
            Assert.Equal(MicrophoneSessionState.Running, session.State);
            Assert.DoesNotContain("disconnect", js.Calls);

            await using var again = recording.Chunks.GetAsyncEnumerator();
            Assert.True(await again.MoveNextAsync());
            Assert.Equal(second, again.Current);
        }
        finally
        {
            MicrophoneJsTestBridge.Bypass.Value = false;
        }
    }

    [Fact]
    public async Task CancellingAPendingReadThenDisposingLeavesThePackageSessionRunning()
    {
        var js = new PackageMicrophoneJsRuntime();
        MicrophoneJsTestBridge.Bypass.Value = true;
        try
        {
            var recorder = new MicrophoneRecorder(js);
            await using var recording = await recorder.StartAsync(CancellationToken.None);
            var session = js.StartedSessions.Single();
            using var cancellation = new CancellationTokenSource();
            var enumerator = recording.Chunks.GetAsyncEnumerator(cancellation.Token);
            var pending = enumerator.MoveNextAsync();
            await Task.Delay(50);
            await cancellation.CancelAsync();

            var canceled = await Record.ExceptionAsync(() => pending.AsTask());
            await enumerator.DisposeAsync();

            Assert.IsAssignableFrom<OperationCanceledException>(canceled);
            Assert.Equal(MicrophoneSessionState.Running, session.State);
            Assert.DoesNotContain("disconnect", js.Calls);
            var payload = LoudChunk();
            session.OnPcmChunk(payload);
            await using var again = recording.Chunks.GetAsyncEnumerator();
            Assert.True(await again.MoveNextAsync());
            Assert.Equal(payload, again.Current);
        }
        finally
        {
            MicrophoneJsTestBridge.Bypass.Value = false;
        }
    }

    [Fact]
    public async Task StoppingWhileTheReaderWaitsCompletesWithoutChannelDisposal()
    {
        var js = new PackageMicrophoneJsRuntime();
        MicrophoneJsTestBridge.Bypass.Value = true;
        try
        {
            var recorder = new MicrophoneRecorder(js);
            await using var recording = await recorder.StartAsync(CancellationToken.None);
            await using var enumerator = recording.Chunks.GetAsyncEnumerator();
            var pending = enumerator.MoveNextAsync();
            await Task.Delay(50);
            await recorder.StopAsync(CancellationToken.None);
            var moved = await pending.AsTask().WaitAsync(TimeSpan.FromSeconds(5));
            Assert.False(moved);
            Assert.Equal(1, js.Calls.Count(call => call == "disconnect"));
            Assert.DoesNotContain("release", js.Calls);
        }
        finally
        {
            MicrophoneJsTestBridge.Bypass.Value = false;
        }
    }

    [Fact]
    public async Task AttachedPackageReaderMarginalAllocationStaysWithinOnePayloadCopyOfLegacyInjection()
    {
        const int sampleRateHz = 16_000;
        const int bytesPerSample = 2;
        const int chunkMs = 20;
        const int frameBytes = sampleRateHz * bytesPerSample * chunkMs / 1000;
        const int smallCount = 20;
        const int largeCount = 40;
        var chunks = new byte[largeCount][];
        for (var index = 0; index < chunks.Length; index++)
        {
            chunks[index] = LoudChunk();
        }

        var legacyPerChunk = await MeasureLegacyMarginalAsync(chunks, smallCount, largeCount);
        var attachedPerChunk = await MeasureAttachedMarginalAsync(chunks, smallCount, largeCount);
        var ceiling = legacyPerChunk + frameBytes + 512;
        _output.WriteLine(
            $"legacyPerChunk={legacyPerChunk} attachedPerChunk={attachedPerChunk} frameBytes={frameBytes} ceiling={ceiling}");
        Assert.True(
            attachedPerChunk > frameBytes / 2,
            $"attached path allocated ~{attachedPerChunk} B/chunk; the package copy is missing from the window.");
        Assert.True(
            attachedPerChunk < ceiling,
            $"attached path allocated ~{attachedPerChunk} B/chunk; legacy ~{legacyPerChunk} B/chunk; ceiling {ceiling} allows one {frameBytes}-byte package copy plus 512 B object overhead.");
    }

    private static async Task<long> MeasureLegacyMarginalAsync(byte[][] chunks, int smallCount, int largeCount)
    {
        await MeasureLegacyAsync(chunks, largeCount);
        await MeasureLegacyAsync(chunks, smallCount);
        await MeasureLegacyAsync(chunks, largeCount);
        var small = await MeasureLegacyAsync(chunks, smallCount);
        var large = await MeasureLegacyAsync(chunks, largeCount);
        return (large - small) / (largeCount - smallCount);
    }

    private static async Task<long> MeasureLegacyAsync(byte[][] chunks, int count)
    {
        var ring = new InMemorySpeechSampleRing(5_000);
        var pump = new SpeechCaptureSessionPump(ring, NullLogger.Instance);
        await using var recording = MicrophoneRecording.CreateLegacy(static (candidate, _) =>
        {
            candidate.OnStopped();
            return Task.CompletedTask;
        });
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < count; index++)
        {
            recording.OnChunk(chunks[index]);
        }

        await recording.StopAsync();
        await pump.RunAsync(
            recording,
            static (_, _) => Task.CompletedTask,
            static _ => { },
            CancellationToken.None);
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    private static async Task<long> MeasureAttachedMarginalAsync(byte[][] chunks, int smallCount, int largeCount)
    {
        var js = new PackageMicrophoneJsRuntime();
        MicrophoneJsTestBridge.Bypass.Value = true;
        try
        {
            var recorder = new MicrophoneRecorder(js);
            await MeasureAttachedAsync(recorder, js, chunks, largeCount);
            await MeasureAttachedAsync(recorder, js, chunks, smallCount);
            await MeasureAttachedAsync(recorder, js, chunks, largeCount);
            var small = await MeasureAttachedAsync(recorder, js, chunks, smallCount);
            var large = await MeasureAttachedAsync(recorder, js, chunks, largeCount);
            return (large - small) / (largeCount - smallCount);
        }
        finally
        {
            MicrophoneJsTestBridge.Bypass.Value = false;
        }
    }

    private static async Task<long> MeasureAttachedAsync(
        MicrophoneRecorder recorder,
        PackageMicrophoneJsRuntime js,
        byte[][] chunks,
        int count)
    {
        var ring = new InMemorySpeechSampleRing(5_000);
        var pump = new SpeechCaptureSessionPump(ring, NullLogger.Instance);
        await using var recording = await recorder.StartAsync(CancellationToken.None);
        var session = js.StartedSessions[^1];
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var index = 0; index < count; index++)
        {
            session.OnPcmChunk(chunks[index]);
        }

        await recorder.StopAsync(CancellationToken.None);
        await pump.RunAsync(
            recording,
            static (_, _) => Task.CompletedTask,
            static _ => { },
            CancellationToken.None);
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    private static byte[] LoudChunk()
    {
        var bytes = new byte[640];
        for (var index = 0; index < bytes.Length; index += 2)
        {
            bytes[index] = 0xA0;
            bytes[index + 1] = 0x0F;
        }

        return bytes;
    }

    private sealed class PackageMicrophoneJsRuntime : IJSRuntime, IJSObjectReference
    {
        private readonly List<string> _calls = [];
        private readonly List<BrowserMicrophoneSession> _sessions = [];
        private IDictionary? _acquirePreferences;
        private int _released;

        public string? AcquireErrorName { get; init; }

        public int ReportedSampleRateHz { get; init; } = 16_000;

        public IReadOnlyList<string> Calls
        {
            get
            {
                lock (_calls)
                {
                    return [.. _calls];
                }
            }
        }

        public IReadOnlyList<BrowserMicrophoneSession> StartedSessions
        {
            get
            {
                lock (_sessions)
                {
                    return [.. _sessions];
                }
            }
        }

        public object? LastAcquirePreference(string name)
            => _acquirePreferences is not null && _acquirePreferences.Contains(name)
                ? _acquirePreferences[name]
                : null;

        public ValueTask<bool> IsLiveAsync()
            => ValueTask.FromResult(Volatile.Read(ref _released) == 0 && _sessions.Count > 0);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
        {
            if (identifier.Contains("micRecorder", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Package bypass via '{identifier}'.");
            }

            cancellationToken.ThrowIfCancellationRequested();
            lock (_calls)
            {
                _calls.Add(identifier);
            }

            return identifier switch
            {
                "import" => ValueTask.FromResult((TValue)(object)this),
                "describe" => ValueTask.FromResult((TValue)(object)new BrowserHostDescription
                {
                    Media = true,
                    Audio = true,
                    Worklet = true,
                }),
                "acquire" => ValueTask.FromResult((TValue)(object)Acquire(args)),
                "start" => ValueTask.FromResult((TValue)(object)Start(args)),
                "isLive" => ValueTask.FromResult((TValue)(object)(Volatile.Read(ref _released) == 0)),
                "trackProcessing" => ValueTask.FromResult((TValue)(object)new BrowserTrackProcessing
                {
                    EchoCancellation = true,
                    NoiseSuppression = false,
                    AutoGainControl = null,
                }),
                "disconnect" or "release" or "stop" or "setSourceEnabled" => CompleteVoid<TValue>(identifier),
                _ => throw new InvalidOperationException($"Unexpected JS invocation '{identifier}'."),
            };
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        private BrowserAcquireResult Acquire(object?[]? args)
        {
            _acquirePreferences = args is { Length: > 0 } ? args[0] as IDictionary : null;
            if (!string.IsNullOrEmpty(AcquireErrorName))
            {
                return new BrowserAcquireResult { ErrorName = AcquireErrorName };
            }

            return new BrowserAcquireResult { SessionId = "pkg-1" };
        }

        private BrowserStartResult Start(object?[]? args)
        {
            var session = Assert.IsType<DotNetObjectReference<BrowserMicrophoneSession>>(args![1]).Value;
            lock (_sessions)
            {
                _sessions.Add(session);
            }

            return new BrowserStartResult
            {
                SampleRateHz = ReportedSampleRateHz,
                Channels = 1,
                ChunkMilliseconds = 20,
            };
        }

        private ValueTask<TValue> CompleteVoid<TValue>(string identifier)
        {
            if (identifier == "release")
            {
                Interlocked.Increment(ref _released);
            }

            return ValueTask.FromResult(default(TValue)!);
        }
    }
}
