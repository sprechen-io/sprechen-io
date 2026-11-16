using Bunit;
using Microsoft.JSInterop;

namespace Sprechen.Speech.Runtime.Tests.Services;

public sealed class MicrophoneRecordingTests : TestContext
{
    [Fact]
    public void PublicSurface_HasNoStaticStartFactoryOutsideTheLifecycleOwner()
    {
        var factories = typeof(MicrophoneRecording)
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(method => method.Name == "StartAsync")
            .ToArray();

        Assert.Empty(factories);
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
        recording.OnStopped();

        Assert.Equal("audio/pcm;rate=16000", recording.MimeType);
    }

    [Fact]
    public async Task Chunks_WhenOnChunkInvoked_YieldsBytes()
    {
        JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true).SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        JSInterop.Setup<string>("micRecorder.connect", _ => true).SetResult("audio/pcm;rate=16000");
        JSInterop.SetupVoid("micRecorder.disconnect", _ => true).SetVoidResult();

        await using var recording = MicrophoneRecording.CreateLegacy(static (_, _) => Task.CompletedTask);

        var enumerator = recording.Chunks.GetAsyncEnumerator();

        Assert.False(recording.Completion.IsCompleted);
        recording.OnChunk(new byte[] { 1, 2 });
        recording.OnStopped();
        Assert.True(recording.Completion.IsCompleted);

        Assert.True(await enumerator.MoveNextAsync());
        Assert.Equal(new byte[] { 1, 2 }, enumerator.Current);
        Assert.False(await enumerator.MoveNextAsync());
    }

    [Fact]
    public async Task Chunks_WhenOnErrorInvoked_Throws()
    {
        JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true).SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        JSInterop.Setup<string>("micRecorder.connect", _ => true).SetResult("audio/pcm;rate=16000");
        JSInterop.SetupVoid("micRecorder.disconnect", _ => true).SetVoidResult();

        await using var recording = MicrophoneRecording.CreateLegacy(static (_, _) => Task.CompletedTask);

        recording.OnError("boom");

        var enumerator = recording.Chunks.GetAsyncEnumerator();
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
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

        var stopTask = recording.StopAsync(CancellationToken.None).AsTask();
        recording.OnStopped();

        await stopTask;
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

        recording.OnStopped();

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

        await using var recording = MicrophoneRecording.CreateLegacy(static (_, _) => Task.CompletedTask);

        var enumerator = recording.Chunks.GetAsyncEnumerator();

        recording.OnChunk(null!);
        recording.OnChunk(Array.Empty<byte>());
        recording.OnChunk(new byte[] { 1, 2 });
        recording.OnStopped();

        Assert.True(await enumerator.MoveNextAsync());
        Assert.Equal(new byte[] { 1, 2 }, enumerator.Current);
        Assert.False(await enumerator.MoveNextAsync());
    }

    [Fact]
    public async Task Chunks_WhenErrorMessageIsWhitespace_UsesDefaultMessage()
    {
        JSInterop.Setup<MicrophoneAcquisitionDto>("micRecorder.acquire", _ => true).SetResult(new MicrophoneAcquisitionDto { SessionId = "1" });
        JSInterop.Setup<string>("micRecorder.connect", _ => true).SetResult("audio/pcm;rate=16000");
        JSInterop.SetupVoid("micRecorder.disconnect", _ => true).SetVoidResult();

        await using var recording = MicrophoneRecording.CreateLegacy(static (_, _) => Task.CompletedTask);

        recording.OnError("  ");

        var enumerator = recording.Chunks.GetAsyncEnumerator();
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            while (await enumerator.MoveNextAsync())
            {
            }
        });

        Assert.Contains("Microphone recorder error.", exception.Message, StringComparison.OrdinalIgnoreCase);
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
}
