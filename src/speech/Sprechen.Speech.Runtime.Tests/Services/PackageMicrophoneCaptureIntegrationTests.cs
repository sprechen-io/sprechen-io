using System.Collections;
using MicroSharp;
using MicroSharp.Browser;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

namespace Sprechen.Speech.Runtime.Tests.Services;

/// <summary>
/// Führt synthetisches PCM durch das Paket in die Aufnahme, den Ring und den Segmentierer.
/// Die Laufzeit spricht nur die Paketschnittstelle und weist micRecorder zurück.
/// </summary>
public sealed class PackageMicrophoneCaptureIntegrationTests
{
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
