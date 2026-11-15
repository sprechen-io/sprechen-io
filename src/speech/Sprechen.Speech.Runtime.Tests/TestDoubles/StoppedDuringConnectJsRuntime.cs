using Microsoft.JSInterop;

namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class StoppedDuringConnectJsRuntime : IJSRuntime
{
    private readonly TaskCompletionSource _connectEntered =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _connectCompletion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private MicrophoneRecording? _recording;
    private int _connectCalls;
    private int _disconnectCalls;

    public int DisconnectCalls => Volatile.Read(ref _disconnectCalls);

    public Task WaitForConnectAsync()
        => _connectEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

    public Task EndBrowserSessionAsync()
        => (_recording ?? throw new InvalidOperationException("The browser callback is not connected."))
            .OnBrowserSessionEnded("MICROPHONE_TRACK_ENDED");

    public void CompleteConnect() => _connectCompletion.TrySetResult();

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public ValueTask<TValue> InvokeAsync<TValue>(
        string identifier,
        CancellationToken cancellationToken,
        object?[]? args)
    {
        return identifier switch
        {
            "micRecorder.acquire" => ValueTask.FromResult(
                (TValue)(object)new MicrophoneAcquisitionDto { SessionId = "1" }),
            "micRecorder.connect" => ConnectAsync<TValue>(args),
            "micRecorder.disconnect" => Disconnect<TValue>(),
            "micRecorder.isLive" => ValueTask.FromResult((TValue)(object)true),
            "micRecorder.setMicGate" => ValueTask.FromResult(default(TValue)!),
            _ => throw new InvalidOperationException($"Unexpected JS invocation '{identifier}'.")
        };
    }

    private async ValueTask<TValue> ConnectAsync<TValue>(object?[]? args)
    {
        var call = Interlocked.Increment(ref _connectCalls);
        _recording = Assert.IsType<DotNetObjectReference<MicrophoneRecording>>(args![1]).Value;
        if (call == 1)
        {
            _connectEntered.TrySetResult();
            await _connectCompletion.Task.ConfigureAwait(false);
        }

        return (TValue)(object)"audio/pcm;rate=16000";
    }

    private ValueTask<TValue> Disconnect<TValue>()
    {
        if (Interlocked.Increment(ref _disconnectCalls) == 1)
        {
            throw new JSException("disconnect failed");
        }

        return ValueTask.FromResult(default(TValue)!);
    }
}
