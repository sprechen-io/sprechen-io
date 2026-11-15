namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

internal sealed class ControlledMicrophoneRecorder : IMicrophoneRecorder
{
    private readonly Action? _onReleaseInvoked;
    private readonly Func<Task>? _beforeReleaseCompletesAsync;
    private readonly Func<Task>? _beforeStopCompletesAsync;
    private readonly Func<int, CancellationToken, Task>? _beforeStartCompletesAsync;
    private readonly Action<int>? _afterStart;

    private MicrophoneRecording? _currentRecording;
    private int _requestAccessCallCount;
    private int _startCallCount;
    private int _stopCallCount;
    private int _releaseCallCount;

    public ControlledMicrophoneRecorder(
        Action? onReleaseInvoked = null,
        Func<Task>? beforeReleaseCompletesAsync = null,
        Func<Task>? beforeStopCompletesAsync = null,
        Func<int, CancellationToken, Task>? beforeStartCompletesAsync = null,
        Action<int>? afterStart = null)
    {
        _onReleaseInvoked = onReleaseInvoked;
        _beforeReleaseCompletesAsync = beforeReleaseCompletesAsync;
        _beforeStopCompletesAsync = beforeStopCompletesAsync;
        _beforeStartCompletesAsync = beforeStartCompletesAsync;
        _afterStart = afterStart;
    }

    public int RequestAccessCallCount => Volatile.Read(ref _requestAccessCallCount);

    public int StartCallCount => Volatile.Read(ref _startCallCount);

    public int StopCallCount => Volatile.Read(ref _stopCallCount);

    public int ReleaseCallCount => Volatile.Read(ref _releaseCallCount);

    public Task RequestAccessAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Interlocked.Increment(ref _requestAccessCallCount);
        return Task.CompletedTask;
    }

    public async Task ReleaseAsync(CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _releaseCallCount);
        _currentRecording = null;
        _onReleaseInvoked?.Invoke();
        if (_beforeReleaseCompletesAsync is not null)
        {
            await _beforeReleaseCompletesAsync().ConfigureAwait(false);
        }
    }

    public async Task<IMicrophoneRecording> StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var startCall = Interlocked.Increment(ref _startCallCount);
        if (_beforeStartCompletesAsync is not null)
        {
            await _beforeStartCompletesAsync(startCall, cancellationToken).ConfigureAwait(false);
        }

        var recording = new MicrophoneRecording(StopRecordingAsync);
        recording.SetMimeType("audio/pcm");
        _currentRecording = recording;
        _afterStart?.Invoke(startCall);
        return recording;
    }

    public void PushChunk(byte[] chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);

        _currentRecording?.OnChunk(chunk);
    }

    public void CompleteRecording()
    {
        _currentRecording?.OnStopped();
    }

    private async Task StopRecordingAsync(MicrophoneRecording recording, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Interlocked.Increment(ref _stopCallCount);
        if (_beforeStopCompletesAsync is not null)
        {
            await _beforeStopCompletesAsync().ConfigureAwait(false);
        }

        recording.OnStopped();
    }
}
