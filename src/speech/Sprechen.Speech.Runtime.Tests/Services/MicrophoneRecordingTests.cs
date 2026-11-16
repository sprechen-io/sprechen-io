using System.Reflection;
using Bunit;
using MicroSharp;
using MicroSharp.Browser;
using Microsoft.JSInterop;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class MicrophoneRecordingTests : TestContext
{
    [Fact]
    public void PublicSurface_HasNoStaticStartFactoryOutsideTheLifecycleOwner()
    {
        var type = typeof(MicrophoneRecording);
        var factories = type
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(method => method.Name == "StartAsync")
            .ToArray();

        Assert.Empty(factories);
        foreach (var name in new[] { "OnChunk", "OnError", "OnStopped", "OnBrowserSessionEnded", "CreateLegacy" })
        {
            Assert.Null(type.GetMethod(
                name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static));
        }
    }

    [Fact]
    public async Task StartAsync_WhenTimesliceIsNonPositive_Throws()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => MicrophoneRecordingTestFactory.StartAsync(
            JSInterop.JSRuntime,
            timesliceMilliseconds: 0,
            CancellationToken.None));
    }

    [Fact]
    public async Task StartAsync_WhenJsReturnsMimeType_TrimsMimeType()
    {
        JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true).SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        JSInterop.Setup<string>("micRecorder.connect", _ => true).SetResult(" audio/pcm;rate=16000 ");
        JSInterop.SetupVoid("micRecorder.disconnect", _ => true).SetVoidResult();

        await using var recording = await MicrophoneRecordingTestFactory.StartAsync(JSInterop.JSRuntime, timesliceMilliseconds: 20, CancellationToken.None);

        Assert.Equal("audio/pcm;rate=16000", recording.MimeType);
    }

    [Fact]
    public async Task Chunks_WhenOnChunkInvoked_YieldsBytes()
    {
        JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true).SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        JSInterop.Setup<string>("micRecorder.connect", _ => true).SetResult("audio/pcm;rate=16000");
        JSInterop.SetupVoid("micRecorder.disconnect", _ => true).SetVoidResult();

        var js = new PackageMicrophoneJsRuntime();
        var recorder = MicrophoneTestRecorder.Create(js);
        await using var recording = await recorder.StartRecordingAsync(20, CancellationToken.None);
        var payload = FullChunk(1);
        var enumerator = recording.Chunks.GetAsyncEnumerator();

        Assert.False(recording.Completion.IsCompleted);
        js.StartedSessions.Single().OnPcmChunk(payload);
        await recording.StopAsync(CancellationToken.None);
        Assert.True(recording.Completion.IsCompleted);

        Assert.True(await enumerator.MoveNextAsync());
        Assert.Equal(payload, enumerator.Current);
        Assert.False(await enumerator.MoveNextAsync());
    }

    [Fact]
    public async Task Chunks_WhenOnErrorInvoked_Throws()
    {
        JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true).SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        JSInterop.Setup<string>("micRecorder.connect", _ => true).SetResult("audio/pcm;rate=16000");
        JSInterop.SetupVoid("micRecorder.disconnect", _ => true).SetVoidResult();

        var js = new PackageMicrophoneJsRuntime();
        var recorder = MicrophoneTestRecorder.Create(js);
        await using var recording = await recorder.StartRecordingAsync(20, CancellationToken.None);
        await js.StartedSessions.Single().OnCaptureFault("boom");

        var enumerator = recording.Chunks.GetAsyncEnumerator();
        var exception = await Assert.ThrowsAsync<MicrophoneException>(async () =>
        {
            while (await enumerator.MoveNextAsync())
            {
            }
        });

        Assert.Contains("boom", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StopAsync_WhenStoppedViaCallback_Completes()
    {
        JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true).SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        JSInterop.Setup<string>("micRecorder.connect", _ => true).SetResult("audio/pcm;rate=16000");
        JSInterop.SetupVoid("micRecorder.disconnect", _ => true).SetVoidResult();

        await using var recording = await MicrophoneRecordingTestFactory.StartAsync(JSInterop.JSRuntime, timesliceMilliseconds: 20, CancellationToken.None);

        await recording.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StopAsync_WhenJsThrows_PropagatesAndAllowsRetry()
    {
        JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true).SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        JSInterop.Setup<string>("micRecorder.connect", _ => true).SetResult("audio/pcm;rate=16000");
        var disconnect = JSInterop.SetupVoid("micRecorder.disconnect", _ => true);
        disconnect.SetException(new JSException("disconnected"));

        await using var recording = await MicrophoneRecordingTestFactory.StartAsync(JSInterop.JSRuntime, timesliceMilliseconds: 20, CancellationToken.None);

        await Assert.ThrowsAsync<JSException>(() => recording.StopAsync(CancellationToken.None).AsTask());
        disconnect.SetVoidResult();

        await recording.StopAsync(CancellationToken.None);

        JSInterop.VerifyInvoke("micRecorder.disconnect", calledTimes: 2);
    }

    [Fact]
    public async Task StopAsync_WhenCalledTwice_WaitsForStop()
    {
        JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true).SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        JSInterop.Setup<string>("micRecorder.connect", _ => true).SetResult("audio/pcm;rate=16000");
        JSInterop.SetupVoid("micRecorder.disconnect", _ => true).SetVoidResult();

        await using var recording = await MicrophoneRecordingTestFactory.StartAsync(JSInterop.JSRuntime, timesliceMilliseconds: 20, CancellationToken.None);

        var firstStop = recording.StopAsync(CancellationToken.None).AsTask();
        var secondStop = recording.StopAsync(CancellationToken.None).AsTask();

        await Task.WhenAll(firstStop, secondStop);
    }

    [Fact]
    public async Task StopAsync_CompletesTheChunkStreamWithoutABrowserCallback()
    {
        JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true).SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        JSInterop.Setup<string>("micRecorder.connect", _ => true).SetResult("audio/pcm;rate=16000");
        JSInterop.SetupVoid("micRecorder.disconnect", _ => true).SetVoidResult();

        await using var recording = await MicrophoneRecordingTestFactory.StartAsync(JSInterop.JSRuntime, timesliceMilliseconds: 20, CancellationToken.None);

        await recording.StopAsync(CancellationToken.None);

        var enumerator = recording.Chunks.GetAsyncEnumerator();
        Assert.False(await enumerator.MoveNextAsync());
    }

    [Fact]
    public async Task StartAsync_WhenBrowserConnectFails_PublishesUnavailable()
    {
        JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true).SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        JSInterop.Setup<string>("micRecorder.connect", _ => true).SetException(new JSException("boom"));
        JSInterop.SetupVoid("micRecorder.release", _ => true).SetVoidResult();

        var failure = await Assert.ThrowsAsync<MicrophoneAcquisitionException>(
            () => MicrophoneRecordingTestFactory.StartAsync(
                JSInterop.JSRuntime,
                timesliceMilliseconds: 20,
                CancellationToken.None));

        Assert.Equal(MicrophoneAcquisitionFailureCode.Unavailable, failure.Code);
        Assert.IsType<JSException>(failure.InnerException);
    }

    [Fact]
    public async Task StopAsync_WhenJsThrowsObjectDisposed_CompletesWithoutThrowing()
    {
        JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true).SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        JSInterop.Setup<string>("micRecorder.connect", _ => true).SetResult("audio/pcm;rate=16000");
        JSInterop.SetupVoid("micRecorder.disconnect", _ => true).SetException(new ObjectDisposedException("js"));

        await using var recording = await MicrophoneRecordingTestFactory.StartAsync(JSInterop.JSRuntime, timesliceMilliseconds: 20, CancellationToken.None);

        await recording.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StopAsync_WhenJsThrowsOperationCanceled_PropagatesAndAllowsRetry()
    {
        JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true).SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        JSInterop.Setup<string>("micRecorder.connect", _ => true).SetResult("audio/pcm;rate=16000");
        var disconnect = JSInterop.SetupVoid("micRecorder.disconnect", _ => true);
        disconnect.SetException(new OperationCanceledException("cancelled"));

        await using var recording = await MicrophoneRecordingTestFactory.StartAsync(JSInterop.JSRuntime, timesliceMilliseconds: 20, CancellationToken.None);

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => recording.StopAsync(CancellationToken.None).AsTask());
        disconnect.SetVoidResult();

        await recording.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Chunks_WhenChunkIsNullOrEmpty_DoesNotYieldChunk()
    {
        JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true).SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        JSInterop.Setup<string>("micRecorder.connect", _ => true).SetResult("audio/pcm;rate=16000");
        JSInterop.SetupVoid("micRecorder.disconnect", _ => true).SetVoidResult();

        var js = new PackageMicrophoneJsRuntime();
        var recorder = MicrophoneTestRecorder.Create(js);
        await using var recording = await recorder.StartRecordingAsync(20, CancellationToken.None);
        var session = js.StartedSessions.Single();
        var payload = FullChunk(2);
        var enumerator = recording.Chunks.GetAsyncEnumerator();

        session.OnPcmChunk(null);
        session.OnPcmChunk(Array.Empty<byte>());
        session.OnPcmChunk(payload);
        await recording.StopAsync(CancellationToken.None);

        Assert.True(await enumerator.MoveNextAsync());
        Assert.Equal(payload, enumerator.Current);
        Assert.False(await enumerator.MoveNextAsync());
    }

    [Fact]
    public async Task Chunks_WhenErrorMessageIsWhitespace_UsesDefaultMessage()
    {
        JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true).SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        JSInterop.Setup<string>("micRecorder.connect", _ => true).SetResult("audio/pcm;rate=16000");
        JSInterop.SetupVoid("micRecorder.disconnect", _ => true).SetVoidResult();

        var js = new PackageMicrophoneJsRuntime();
        var recorder = MicrophoneTestRecorder.Create(js);
        await using var recording = await recorder.StartRecordingAsync(20, CancellationToken.None);
        await js.StartedSessions.Single().OnCaptureFault("  ");

        var enumerator = recording.Chunks.GetAsyncEnumerator();
        var exception = await Assert.ThrowsAsync<MicrophoneException>(async () =>
        {
            while (await enumerator.MoveNextAsync())
            {
            }
        });

        Assert.Contains("Browser microphone capture failed.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DisposeAsync_WhenStopThrowsUnexpectedException_PropagatesAndAllowsRetry()
    {
        JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true).SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        JSInterop.Setup<string>("micRecorder.connect", _ => true).SetResult("audio/pcm;rate=16000");
        var disconnect = JSInterop.SetupVoid("micRecorder.disconnect", _ => true);
        disconnect.SetException(new InvalidOperationException("unexpected"));

        var recording = await MicrophoneRecordingTestFactory.StartAsync(JSInterop.JSRuntime, timesliceMilliseconds: 20, CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() => recording.DisposeAsync().AsTask());
        disconnect.SetVoidResult();

        await recording.DisposeAsync();

        JSInterop.VerifyInvoke("micRecorder.disconnect", calledTimes: 2);
    }

    private static byte[] FullChunk(byte marker)
    {
        var bytes = new byte[new PcmFormat(16_000, TimeSpan.FromMilliseconds(20)).BytesPerChunk];
        bytes[0] = marker;
        return bytes;
    }
}
