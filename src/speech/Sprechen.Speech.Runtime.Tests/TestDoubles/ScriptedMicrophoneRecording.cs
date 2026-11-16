using System.Threading.Channels;

namespace Sprechen.Speech.Runtime.Tests.TestDoubles;

/// <summary>
/// Test-only recording that injects chunks without a browser session.
/// Production capture does not use this channel.
/// </summary>
internal sealed class ScriptedMicrophoneRecording : IMicrophoneRecording
{
    private static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(5);

    private readonly object _sync = new();
    private readonly Func<ScriptedMicrophoneRecording, CancellationToken, Task> _stopAsync;
    private readonly Channel<byte[]> _chunks = Channel.CreateUnbounded<byte[]>(new UnboundedChannelOptions
    {
        SingleReader = true,
    });

    private readonly TaskCompletionSource _stopped = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private TaskCompletionSource? _stopAttempt;
    private bool _disposed;

    private ScriptedMicrophoneRecording(Func<ScriptedMicrophoneRecording, CancellationToken, Task> stopAsync)
    {
        _stopAsync = stopAsync ?? throw new ArgumentNullException(nameof(stopAsync));
    }

    public static ScriptedMicrophoneRecording Create(
        Func<ScriptedMicrophoneRecording, CancellationToken, Task> stopAsync)
        => new(stopAsync);

    public string MimeType { get; private set; } = string.Empty;

    public IAsyncEnumerable<byte[]> Chunks => _chunks.Reader.ReadAllAsync();

    public Task Completion => _stopped.Task;

    public void SetMimeType(string? mimeType)
    {
        MimeType = mimeType?.Trim() ?? string.Empty;
    }

    public void OnChunk(byte[]? bytes)
    {
        if (_disposed || bytes is null || bytes.Length == 0)
        {
            return;
        }

        _chunks.Writer.TryWrite(bytes);
    }

    public void OnError(string? message)
    {
        if (_disposed)
        {
            return;
        }

        var exception = new InvalidOperationException(
            string.IsNullOrWhiteSpace(message) ? "Microphone recorder error." : message);
        _chunks.Writer.TryComplete(exception);
        _stopped.TrySetResult();
    }

    public void OnStopped()
    {
        if (_disposed)
        {
            return;
        }

        _chunks.Writer.TryComplete();
        _stopped.TrySetResult();
    }

    public ValueTask StopAsync() => StopAsync(CancellationToken.None);

    public async ValueTask StopAsync(CancellationToken cancellationToken)
    {
        TaskCompletionSource stopAttempt;
        var startsAttempt = false;
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            if (_stopAttempt is null)
            {
                _stopAttempt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                startsAttempt = true;
            }

            stopAttempt = _stopAttempt;
        }

        if (startsAttempt)
        {
            _ = CompleteStopAttemptAsync(stopAttempt);
        }

        await stopAttempt.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        lock (_sync)
        {
            _disposed = true;
        }

        OnStopped();
    }

    private async Task CompleteStopAttemptAsync(TaskCompletionSource stopAttempt)
    {
        try
        {
            await _stopAsync(this, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            lock (_sync)
            {
                if (ReferenceEquals(_stopAttempt, stopAttempt))
                {
                    _stopAttempt = null;
                }
            }

            stopAttempt.TrySetException(exception);
            return;
        }

        var completed = await Task.WhenAny(_stopped.Task, Task.Delay(StopTimeout)).ConfigureAwait(false);
        if (completed != _stopped.Task)
        {
            _chunks.Writer.TryComplete(new TimeoutException("Microphone recorder stop timed out."));
            _stopped.TrySetResult();
        }

        stopAttempt.TrySetResult();
    }
}
